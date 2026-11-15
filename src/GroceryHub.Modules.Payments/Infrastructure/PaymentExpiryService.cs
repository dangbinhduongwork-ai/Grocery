using System.Text.Json;
using GroceryHub.Modules.Payments.Configuration;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GroceryHub.Modules.Payments.Infrastructure;

public sealed class PaymentExpiryService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentExpiryService> _logger;
    private readonly TimeProvider _timeProvider;

    public PaymentExpiryService(
        IServiceProvider serviceProvider,
        ILogger<PaymentExpiryService> logger,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _serviceProvider = serviceProvider;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PaymentExpiryService started.");

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1), _timeProvider);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await SweepExpiredPaymentsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error occurred during PaymentExpiryService sweep.");
            }
        }
    }

    public async Task<int> SweepExpiredPaymentsAsync(CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GroceryHubDbContext>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<SePayOptions>>().Value;

        DateTimeOffset now = _timeProvider.GetUtcNow();
        int graceMinutes = options.ExpiryGraceMinutes >= 0 ? options.ExpiryGraceMinutes : 5;

        // Query orders in PendingPayment with SepayQr whose latest Open attempt has expired beyond grace
        var expiredAttempts = await db.PaymentAttempts
            .Include(pa => pa.Payment)
            .Where(pa => pa.Status == PaymentAttemptStatusConstants.Open &&
                         pa.Payment != null &&
                         pa.Payment.Method == PaymentMethodConstants.SepayQr &&
                         pa.Payment.Status == PaymentStatusConstants.Pending &&
                         now > pa.ExpiresAt.AddMinutes(graceMinutes))
            .ToListAsync(ct);

        if (expiredAttempts.Count == 0)
        {
            return 0;
        }

        int expiredCount = 0;

        foreach (var attempt in expiredAttempts)
        {
            if (attempt.Payment is null) continue;

            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            try
            {
                var order = await db.Orders
                    .Include(o => o.StatusHistory)
                    .FirstOrDefaultAsync(o => o.Id == attempt.Payment.OrderId, ct);

                if (order is null || !string.Equals(order.Status, OrderStatusConstants.PendingPayment, StringComparison.OrdinalIgnoreCase))
                {
                    // Order was already paid, cancelled, or confirmed
                    attempt.Status = PaymentAttemptStatusConstants.Expired;
                    await db.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                    continue;
                }

                // 1. Mark all attempts on this payment as Expired
                var allAttempts = await db.PaymentAttempts
                    .Where(pa => pa.PaymentId == attempt.PaymentId)
                    .ToListAsync(ct);

                foreach (var a in allAttempts)
                {
                    a.Status = PaymentAttemptStatusConstants.Expired;
                }

                // 2. Mark Payment as Expired
                attempt.Payment.Status = PaymentStatusConstants.Expired;
                attempt.Payment.UpdatedAt = now;

                // 3. Mark Order as Cancelled
                order.Status = OrderStatusConstants.Cancelled;
                order.PaymentStatus = OrderPaymentStatusConstants.Expired;
                order.CancelledAt = now;
                order.CancellationReason = "PaymentExpired";
                order.UpdatedAt = now;

                var expiryHistory = new OrderStatusHistory
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    FromStatus = OrderStatusConstants.PendingPayment,
                    ToStatus = OrderStatusConstants.Cancelled,
                    Reason = "Hết thời gian thanh toán VietQR (PaymentExpired)",
                    ActorUserId = null,
                    ActorRole = "System",
                    CreatedAt = now
                };
                db.OrderStatusHistories.Add(expiryHistory);
                order.StatusHistory.Add(expiryHistory);

                // 4. Release stock reservations
                var stockService = scope.ServiceProvider.GetRequiredService<IStockPersistenceService>();
                await stockService.ReleaseStockReservationAsync(order.Id, ct);

                // 5. Release coupon redemption if any
                var couponRedemption = await db.CouponRedemptions
                    .FirstOrDefaultAsync(cr => cr.OrderId == order.Id && cr.ReleasedAt == null, ct);

                if (couponRedemption is not null)
                {
                    couponRedemption.ReleasedAt = now;
                    var coupon = await db.Coupons.FirstOrDefaultAsync(c => c.Id == couponRedemption.CouponId, ct);
                    if (coupon is not null && coupon.UsedCount > 0)
                    {
                        coupon.UsedCount--;
                        coupon.UpdatedAt = now;
                    }
                }

                // 6. Audit log
                db.AuditLogs.Add(new AuditLog
                {
                    Id = 0,
                    ActorUserId = null,
                    ActorRole = "System",
                    Action = "OrderCancelledByExpiry",
                    EntityType = "Order",
                    EntityId = order.Id.ToString(),
                    AfterJson = JsonSerializer.Serialize(new { order.OrderNumber, Reason = "PaymentExpired" }),
                    At = now
                });

                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                expiredCount++;
                _logger.LogInformation("Cancelled expired QR order {OrderNumber} (PaymentCode: {Code})", order.OrderNumber, attempt.PaymentCode);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Failed to cancel expired order {OrderId}", attempt.Payment.OrderId);
            }
        }

        return expiredCount;
    }
}

