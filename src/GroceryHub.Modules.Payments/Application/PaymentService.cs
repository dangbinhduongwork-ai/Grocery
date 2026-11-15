using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using GroceryHub.Modules.Payments.Configuration;
using GroceryHub.Modules.Payments.Contracts;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GroceryHub.Modules.Payments.Application;

public interface IPaymentService
{
    Task<Result<PaymentStatusResponseDto>> GetPaymentStatusByCodeAsync(
        string paymentCode,
        Guid? currentUserId,
        bool isStaff,
        CancellationToken ct = default);

    Task<Result<PaymentAttemptDto>> CreateNewPaymentAttemptAsync(
        Guid orderId,
        Guid currentUserId,
        string? idempotencyKey,
        CancellationToken ct = default);

    Task<(int StatusCode, bool Success, string? Error)> ProcessWebhookAsync(
        string rawBody,
        string? timestampHeader,
        string? signatureHeader,
        string? authHeader,
        string? clientIp,
        CancellationToken ct = default);

    Task<Result<PagedResponse<PaymentTransactionDto>>> GetPaymentTransactionsAsync(
        string? reviewStatus,
        string? matchStatus,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<Result> ResolvePaymentTransactionAsync(
        Guid transactionId,
        ResolvePaymentTransactionRequest request,
        Guid resolvedByUserId,
        CancellationToken ct = default);

    Task<Result<PagedResponse<RefundDto>>> GetRefundsAsync(
        string? status,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<Result<RefundDto>> RequestRefundAsync(
        Guid orderId,
        long amount,
        string reason,
        Guid requestedByUserId,
        CancellationToken ct = default);

    Task<Result> ApproveRefundAsync(
        Guid refundId,
        string? note,
        Guid decidedByUserId,
        CancellationToken ct = default);

    Task<Result> RejectRefundAsync(
        Guid refundId,
        string reason,
        Guid decidedByUserId,
        CancellationToken ct = default);

    Task<Result> CompleteRefundAsync(
        Guid refundId,
        string bankReference,
        string? note,
        Guid completedByUserId,
        CancellationToken ct = default);

    Task<Result<string>> SimulateWebhookPayloadAsync(
        SimulateWebhookRequest request,
        CancellationToken ct = default);
}

public sealed class PaymentService : IPaymentService
{
    private static readonly Regex PaymentCodeRegex = new(@"\bGH\d{10}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly GroceryHubDbContext _db;
    private readonly SePayOptions _options;
    private readonly IVietQrUrlBuilder _qrUrlBuilder;
    private readonly ISePaySignatureValidator _signatureValidator;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        GroceryHubDbContext db,
        IOptions<SePayOptions> options,
        IVietQrUrlBuilder qrUrlBuilder,
        ISePaySignatureValidator signatureValidator,
        TimeProvider timeProvider,
        ILogger<PaymentService> logger)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(qrUrlBuilder);
        ArgumentNullException.ThrowIfNull(signatureValidator);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _db = db;
        _options = options.Value;
        _qrUrlBuilder = qrUrlBuilder;
        _signatureValidator = signatureValidator;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Result<PaymentStatusResponseDto>> GetPaymentStatusByCodeAsync(
        string paymentCode,
        Guid? currentUserId,
        bool isStaff,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(paymentCode))
        {
            return Result.Failure<PaymentStatusResponseDto>(DomainError.NotFound(
                ErrorCodes.NotFound, "Payment code is required."));
        }

        string normalizedCode = paymentCode.Trim().ToUpperInvariant();

        var attempt = await _db.PaymentAttempts
            .AsNoTracking()
            .Include(pa => pa.Payment)
            .FirstOrDefaultAsync(pa => pa.PaymentCode == normalizedCode, ct);

        if (attempt?.Payment is null)
        {
            return Result.Failure<PaymentStatusResponseDto>(DomainError.NotFound(
                ErrorCodes.NotFound, $"Payment with code '{normalizedCode}' was not found."));
        }

        var order = await _db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == attempt.Payment.OrderId, ct);

        if (order is null)
        {
            return Result.Failure<PaymentStatusResponseDto>(DomainError.NotFound(
                ErrorCodes.NotFound, "Associated order was not found."));
        }

        if (!isStaff && (currentUserId is null || order.UserId != currentUserId.Value))
        {
            return Result.Failure<PaymentStatusResponseDto>(DomainError.Forbidden(
                ErrorCodes.Forbidden, "You do not have permission to view this payment."));
        }

        int maxAttempts = _options.MaxAttemptsPerOrder > 0 ? _options.MaxAttemptsPerOrder : 5;
        int totalAttemptsCount = await _db.PaymentAttempts
            .CountAsync(pa => pa.PaymentId == attempt.PaymentId, ct);

        bool canRetry = string.Equals(order.Status, OrderStatusConstants.PendingPayment, StringComparison.OrdinalIgnoreCase) &&
                        totalAttemptsCount < maxAttempts;

        var dto = new PaymentStatusResponseDto(
            normalizedCode,
            attempt.Payment.Status,
            attempt.Payment.Method,
            attempt.Payment.Amount,
            attempt.Payment.PaidAt,
            order.Status,
            attempt.ExpiresAt,
            canRetry,
            attempt.AttemptNumber,
            maxAttempts
        );

        return Result.Success(dto);
    }

    public async Task<Result<PaymentAttemptDto>> CreateNewPaymentAttemptAsync(
        Guid orderId,
        Guid currentUserId,
        string? idempotencyKey,
        CancellationToken ct = default)
    {
        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
        {
            return Result.Failure<PaymentAttemptDto>(DomainError.NotFound(
                ErrorCodes.NotFound, "Order was not found."));
        }

        if (order.UserId != currentUserId)
        {
            return Result.Failure<PaymentAttemptDto>(DomainError.Forbidden(
                ErrorCodes.Forbidden, "You do not own this order."));
        }

        var payment = await _db.Payments
            .Include(p => p.Attempts)
            .FirstOrDefaultAsync(p => p.OrderId == orderId, ct);

        if (!string.Equals(order.Status, OrderStatusConstants.PendingPayment, StringComparison.OrdinalIgnoreCase) ||
            payment is null ||
            !string.Equals(payment.Method, PaymentMethodConstants.SepayQr, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<PaymentAttemptDto>(DomainError.Conflict(
                "ORDER_NOT_IN_PENDING_PAYMENT",
                "New payment attempts can only be generated for orders in PendingPayment with SePay VietQR."));
        }

        int maxAttempts = _options.MaxAttemptsPerOrder > 0 ? _options.MaxAttemptsPerOrder : 5;
        if (payment.Attempts.Count >= maxAttempts)
        {
            return Result.Failure<PaymentAttemptDto>(DomainError.Conflict(
                "PAYMENT_ATTEMPT_LIMIT_REACHED",
                $"Maximum payment attempts limit ({maxAttempts}) reached for this order."));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();

        // Supersede any existing Open attempt
        foreach (var existingAttempt in payment.Attempts)
        {
            if (string.Equals(existingAttempt.Status, PaymentAttemptStatusConstants.Open, StringComparison.OrdinalIgnoreCase))
            {
                existingAttempt.Status = PaymentAttemptStatusConstants.Superseded;
            }
        }

        long paymentSeq = await GetNextSequenceValueAsync("[payments].[PaymentCodeSeq]", ct);
        string newCode = $"GH{paymentSeq:D10}";
        int expiryMinutes = _options.QrExpiryMinutes > 0 ? _options.QrExpiryMinutes : 30;
        DateTimeOffset expiresAt = now.AddMinutes(expiryMinutes);

        var newAttempt = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            PaymentCode = newCode,
            AttemptNumber = payment.Attempts.Count + 1,
            Amount = payment.Amount,
            Status = PaymentAttemptStatusConstants.Open,
            ExpiresAt = expiresAt,
            CreatedAt = now
        };

        _db.PaymentAttempts.Add(newAttempt);
        payment.Attempts.Add(newAttempt);
        payment.PaymentCode = newCode;
        payment.UpdatedAt = now;

        await _db.SaveChangesAsync(ct);

        string transferContent = string.IsNullOrWhiteSpace(_options.TransferContentPrefix)
            ? newCode
            : $"{_options.TransferContentPrefix}{newCode}";

        string qrUrl = _qrUrlBuilder.BuildQrUrl(
            _options.AccountNumber,
            _options.BankShortName,
            payment.Amount,
            transferContent);

        var qrDetails = new QrPaymentDetailsDto(
            qrUrl,
            _options.BankShortName,
            _options.AccountNumber,
            _options.AccountName,
            payment.Amount,
            transferContent,
            expiresAt
        );

        var resultDto = new PaymentAttemptDto(
            newAttempt.Id,
            newAttempt.PaymentId,
            newAttempt.PaymentCode,
            newAttempt.AttemptNumber,
            newAttempt.Amount,
            newAttempt.Status,
            newAttempt.ExpiresAt,
            qrDetails
        );

        return Result.Success(resultDto);
    }

    public async Task<(int StatusCode, bool Success, string? Error)> ProcessWebhookAsync(
        string rawBody,
        string? timestampHeader,
        string? signatureHeader,
        string? authHeader,
        string? clientIp,
        CancellationToken ct = default)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();

        // 1. Check body size limit (64 KB)
        int maxBytes = _options.Webhook.MaxBodyBytes > 0 ? _options.Webhook.MaxBodyBytes : 65536;
        if (System.Text.Encoding.UTF8.GetByteCount(rawBody ?? string.Empty) > maxBytes)
        {
            _logger.LogWarning("SePay webhook rejected: body size exceeds limit of {MaxBytes} bytes from IP {Ip}", maxBytes, clientIp);
            return (413, false, "Payload Too Large");
        }

        // 2. Cryptographic signature / API key verification
        bool isValidSignature = _signatureValidator.ValidateWebhook(rawBody ?? string.Empty, timestampHeader, signatureHeader, authHeader, now);
        if (!isValidSignature)
        {
            _logger.LogWarning("SePay webhook rejected: invalid authentication signature from IP {Ip}", clientIp);
            return (401, false, "Unauthorized");
        }

        // 3. Parse JSON payload
        SePayWebhookPayloadDto? payload;
        try
        {
            payload = JsonSerializer.Deserialize<SePayWebhookPayloadDto>(rawBody ?? string.Empty);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "SePay webhook rejected: malformed JSON from IP {Ip}", clientIp);
            return (400, false, "Malformed JSON");
        }

        if (payload is null || payload.Id < 0 || payload.TransferAmount <= 0 || string.IsNullOrWhiteSpace(payload.AccountNumber))
        {
            _logger.LogWarning("SePay webhook rejected: missing required fields from IP {Ip}", clientIp);
            return (400, false, "Invalid payload fields");
        }

        string providerTxId = payload.Id.ToString(CultureInfo.InvariantCulture);

        // 4. Ingest into WebhookEvents first (Idempotency anchor)
        WebhookEvent webhookEvent;
        var existingEvent = await _db.WebhookEvents
            .FirstOrDefaultAsync(we => we.Provider == "SEPAY" && we.ProviderTransactionId == providerTxId, ct);

        if (existingEvent is not null)
        {
            if (existingEvent.ProcessingStatus == WebhookProcessingStatusConstants.Processed ||
                existingEvent.ProcessingStatus == WebhookProcessingStatusConstants.Ignored)
            {
                existingEvent.DeliveryCount++;
                try { await _db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { }
                _logger.LogInformation("SePay webhook duplicate ignored for tx {TxId}, DeliveryCount = {Count}", providerTxId, existingEvent.DeliveryCount);
                return (200, true, null);
            }

            // Status is Received: another worker is currently processing this transaction
            _db.Entry(existingEvent).State = EntityState.Detached;
            for (int poll = 0; poll < 20; poll++)
            {
                await Task.Delay(100, ct);
                var concurrentEvent = await _db.WebhookEvents
                    .AsNoTracking()
                    .FirstOrDefaultAsync(we => we.Provider == "SEPAY" && we.ProviderTransactionId == providerTxId, ct);

                if (concurrentEvent is not null &&
                    (concurrentEvent.ProcessingStatus == WebhookProcessingStatusConstants.Processed ||
                     concurrentEvent.ProcessingStatus == WebhookProcessingStatusConstants.Ignored))
                {
                    _logger.LogInformation("Concurrent duplicate webhook resolved after in-flight worker completed for tx {TxId}", providerTxId);
                    return (200, true, null);
                }
            }

            _logger.LogInformation("Concurrent in-flight webhook acknowledged for tx {TxId}", providerTxId);
            return (200, true, null);
        }
        else
        {
            webhookEvent = new WebhookEvent
            {
                Id = Guid.NewGuid(),
                Provider = "SEPAY",
                ProviderTransactionId = providerTxId,
                RawBody = rawBody ?? string.Empty,
                SignatureValid = true,
                ReceivedAt = now,
                ProcessingStatus = WebhookProcessingStatusConstants.Received,
                DeliveryCount = 1
            };
            _db.WebhookEvents.Add(webhookEvent);
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Concurrency race: another worker won the insert for this unique (Provider, ProviderTransactionId)
                // Detach the failed entity to prevent change tracker corruption
                _db.Entry(webhookEvent).State = EntityState.Detached;

                // Wait briefly for the winning worker to complete processing
                for (int poll = 0; poll < 20; poll++)
                {
                    await Task.Delay(100, ct);
                    var concurrentEvent = await _db.WebhookEvents
                        .AsNoTracking()
                        .FirstOrDefaultAsync(we => we.Provider == "SEPAY" && we.ProviderTransactionId == providerTxId, ct);

                    if (concurrentEvent is not null &&
                        (concurrentEvent.ProcessingStatus == WebhookProcessingStatusConstants.Processed ||
                         concurrentEvent.ProcessingStatus == WebhookProcessingStatusConstants.Ignored))
                    {
                        _logger.LogInformation("Concurrent duplicate webhook resolved after winning worker completed for tx {TxId}", providerTxId);
                        return (200, true, null);
                    }
                }

                _logger.LogInformation("Concurrent duplicate webhook acknowledged for tx {TxId}", providerTxId);
                return (200, true, null);
            }
        }

        // 5. Parse TransactionTime
        DateTimeOffset transactionTime = now;
        if (!string.IsNullOrWhiteSpace(payload.TransactionDate) &&
            DateTime.TryParseExact(payload.TransactionDate, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDt))
        {
            transactionTime = new DateTimeOffset(parsedDt, TimeSpan.FromHours(7));
        }

        // 6. Handle Outgoing Transfers
        if (!string.Equals(payload.TransferType, "in", StringComparison.OrdinalIgnoreCase))
        {
            var outTx = new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                WebhookEventId = webhookEvent.Id,
                PaymentAttemptId = null,
                Direction = "out",
                Amount = payload.TransferAmount,
                AccountNumber = payload.AccountNumber,
                Gateway = payload.Gateway,
                ReferenceCode = payload.ReferenceCode,
                Content = payload.Content,
                TransactionTime = transactionTime,
                MatchStatus = MatchStatusConstants.OutgoingIgnored,
                ReviewStatus = PaymentReviewStatusConstants.None
            };
            _db.PaymentTransactions.Add(outTx);
            webhookEvent.ProcessingStatus = WebhookProcessingStatusConstants.Ignored;
            webhookEvent.ProcessedAt = now;
            await _db.SaveChangesAsync(ct);
            return (200, true, null);
        }

        // 7. Extract Payment Code
        string? extractedCode = null;
        if (!string.IsNullOrWhiteSpace(payload.Code))
        {
            extractedCode = payload.Code.Trim().ToUpperInvariant();
        }
        else if (!string.IsNullOrWhiteSpace(payload.Content))
        {
            var match = PaymentCodeRegex.Match(payload.Content);
            if (match.Success)
            {
                extractedCode = match.Value.ToUpperInvariant();
            }
        }

        // 8. Matching & Decision Matrix inside ACID transaction
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Case 1: Wrong receiving account
            bool accountMatches = string.Equals(payload.AccountNumber, _options.AccountNumber, StringComparison.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(_options.SubAccount) && !string.IsNullOrWhiteSpace(payload.SubAccount))
            {
                accountMatches = accountMatches || string.Equals(payload.SubAccount, _options.SubAccount, StringComparison.OrdinalIgnoreCase);
            }

            if (!accountMatches)
            {
                RecordAnomalyTransaction(webhookEvent.Id, null, payload, transactionTime, MatchStatusConstants.WrongAccount, PaymentReviewStatusConstants.NeedsReview);
                webhookEvent.ProcessingStatus = WebhookProcessingStatusConstants.Processed;
                webhookEvent.ProcessedAt = now;
                await _db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return (200, true, null);
            }

            // Case 2: Missing or Unknown Payment Code
            if (string.IsNullOrWhiteSpace(extractedCode))
            {
                RecordAnomalyTransaction(webhookEvent.Id, null, payload, transactionTime, MatchStatusConstants.UnknownCode, PaymentReviewStatusConstants.NeedsReview);
                webhookEvent.ProcessingStatus = WebhookProcessingStatusConstants.Processed;
                webhookEvent.ProcessedAt = now;
                await _db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return (200, true, null);
            }

            var attempt = await _db.PaymentAttempts
                .Include(pa => pa.Payment)
                .FirstOrDefaultAsync(pa => pa.PaymentCode == extractedCode, ct);

            if (attempt?.Payment is null)
            {
                RecordAnomalyTransaction(webhookEvent.Id, null, payload, transactionTime, MatchStatusConstants.UnknownCode, PaymentReviewStatusConstants.NeedsReview);
                webhookEvent.ProcessingStatus = WebhookProcessingStatusConstants.Processed;
                webhookEvent.ProcessedAt = now;
                await _db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return (200, true, null);
            }

            var order = await _db.Orders
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == attempt.Payment.OrderId, ct);

            if (order is null)
            {
                RecordAnomalyTransaction(webhookEvent.Id, attempt.Id, payload, transactionTime, MatchStatusConstants.UnknownCode, PaymentReviewStatusConstants.NeedsReview);
                webhookEvent.ProcessingStatus = WebhookProcessingStatusConstants.Processed;
                webhookEvent.ProcessedAt = now;
                await _db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return (200, true, null);
            }

            // Case 3: Duplicate Payment
            bool isAlreadyPaid = string.Equals(attempt.Payment.Status, PaymentStatusConstants.Paid, StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(attempt.Status, PaymentAttemptStatusConstants.Paid, StringComparison.OrdinalIgnoreCase) ||
                                 (!string.Equals(order.Status, OrderStatusConstants.PendingPayment, StringComparison.OrdinalIgnoreCase) &&
                                  !string.Equals(order.Status, OrderStatusConstants.Cancelled, StringComparison.OrdinalIgnoreCase));

            if (isAlreadyPaid)
            {
                RecordAnomalyTransaction(webhookEvent.Id, attempt.Id, payload, transactionTime, MatchStatusConstants.DuplicatePayment, PaymentReviewStatusConstants.NeedsReview);
                webhookEvent.ProcessingStatus = WebhookProcessingStatusConstants.Processed;
                webhookEvent.ProcessedAt = now;
                await _db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return (200, true, null);
            }

            // Case 4: Late After Expiry
            int graceMinutes = _options.ExpiryGraceMinutes >= 0 ? _options.ExpiryGraceMinutes : 5;
            bool isLate = string.Equals(order.Status, OrderStatusConstants.Cancelled, StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(attempt.Payment.Status, PaymentStatusConstants.Expired, StringComparison.OrdinalIgnoreCase) ||
                          now > attempt.ExpiresAt.AddMinutes(graceMinutes);

            if (isLate)
            {
                RecordAnomalyTransaction(webhookEvent.Id, attempt.Id, payload, transactionTime, MatchStatusConstants.LateAfterExpiry, PaymentReviewStatusConstants.NeedsReview);
                webhookEvent.ProcessingStatus = WebhookProcessingStatusConstants.Processed;
                webhookEvent.ProcessedAt = now;
                await _db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return (200, true, null);
            }

            // Case 5: Underpaid or Overpaid
            if (payload.TransferAmount < attempt.Payment.Amount)
            {
                RecordAnomalyTransaction(webhookEvent.Id, attempt.Id, payload, transactionTime, MatchStatusConstants.Underpaid, PaymentReviewStatusConstants.NeedsReview);
                webhookEvent.ProcessingStatus = WebhookProcessingStatusConstants.Processed;
                webhookEvent.ProcessedAt = now;
                await _db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return (200, true, null);
            }

            if (payload.TransferAmount > attempt.Payment.Amount)
            {
                RecordAnomalyTransaction(webhookEvent.Id, attempt.Id, payload, transactionTime, MatchStatusConstants.Overpaid, PaymentReviewStatusConstants.NeedsReview);
                webhookEvent.ProcessingStatus = WebhookProcessingStatusConstants.Processed;
                webhookEvent.ProcessedAt = now;
                await _db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return (200, true, null);
            }

            // Case 6: EXACT MATCH — Confirm Order & Mark Payment Paid!
            attempt.Status = PaymentAttemptStatusConstants.Paid;

            // Mark any other attempts on this payment as Expired
            var otherAttempts = await _db.PaymentAttempts
                .Where(pa => pa.PaymentId == attempt.PaymentId && pa.Id != attempt.Id)
                .ToListAsync(ct);
            foreach (var oa in otherAttempts)
            {
                oa.Status = PaymentAttemptStatusConstants.Expired;
            }

            attempt.Payment.Status = PaymentStatusConstants.Paid;
            attempt.Payment.PaidAt = transactionTime;
            attempt.Payment.UpdatedAt = now;

            order.Status = OrderStatusConstants.Confirmed;
            order.PaymentStatus = OrderPaymentStatusConstants.Paid;
            order.UpdatedAt = now;

            // Commit Stock Reservations
            var reservations = await _db.StockReservations
                .Where(r => r.OrderId == order.Id && r.Status == "Reserved")
                .ToListAsync(ct);
            foreach (var r in reservations)
            {
                r.Status = "Active";
                r.ExpiresAt = null;
            }

            var confirmedHistory = new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                FromStatus = OrderStatusConstants.PendingPayment,
                ToStatus = OrderStatusConstants.Confirmed,
                Reason = "Thanh toán thành công qua SePay VietQR",
                ActorUserId = null,
                ActorRole = "System",
                CreatedAt = now
            };
            _db.OrderStatusHistories.Add(confirmedHistory);
            order.StatusHistory.Add(confirmedHistory);

            _db.AuditLogs.Add(new AuditLog
            {
                Id = 0,
                ActorUserId = null,
                ActorRole = "System",
                Action = "OrderConfirmedByPayment",
                EntityType = "Order",
                EntityId = order.Id.ToString(),
                AfterJson = JsonSerializer.Serialize(new { order.OrderNumber, order.Total, attempt.PaymentCode, payload.Id }),
                At = now
            });

            _db.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = "OrderConfirmed",
                Payload = JsonSerializer.Serialize(new { OrderId = order.Id, OrderNumber = order.OrderNumber, PaidAt = transactionTime }),
                OccurredAt = now,
                ProcessedAt = null
            });

            var matchedTx = new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                WebhookEventId = webhookEvent.Id,
                PaymentAttemptId = attempt.Id,
                Direction = "in",
                Amount = payload.TransferAmount,
                AccountNumber = payload.AccountNumber,
                Gateway = payload.Gateway,
                ReferenceCode = payload.ReferenceCode,
                Content = payload.Content,
                TransactionTime = transactionTime,
                MatchStatus = MatchStatusConstants.Matched,
                ReviewStatus = PaymentReviewStatusConstants.None
            };
            _db.PaymentTransactions.Add(matchedTx);

            webhookEvent.ProcessingStatus = WebhookProcessingStatusConstants.Processed;
            webhookEvent.ProcessedAt = now;

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation("SePay payment matched and order confirmed: Order {OrderNumber}, Code {Code}, Amount {Amount}",
                order.OrderNumber, attempt.PaymentCode, payload.TransferAmount);

            return (200, true, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Unexpected error processing SePay webhook event {EventId}: {Message}", webhookEvent.Id, ex.Message);
            return (500, false, "Internal Server Error");
        }
    }

    private void RecordAnomalyTransaction(
        Guid webhookEventId,
        Guid? attemptId,
        SePayWebhookPayloadDto payload,
        DateTimeOffset txTime,
        string matchStatus,
        string reviewStatus)
    {
        var anomaly = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            WebhookEventId = webhookEventId,
            PaymentAttemptId = attemptId,
            Direction = "in",
            Amount = payload.TransferAmount,
            AccountNumber = payload.AccountNumber,
            Gateway = payload.Gateway,
            ReferenceCode = payload.ReferenceCode,
            Content = payload.Content,
            TransactionTime = txTime,
            MatchStatus = matchStatus,
            ReviewStatus = reviewStatus
        };
        _db.PaymentTransactions.Add(anomaly);
    }

    public async Task<Result<PagedResponse<PaymentTransactionDto>>> GetPaymentTransactionsAsync(
        string? reviewStatus,
        string? matchStatus,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        var query = _db.PaymentTransactions
            .AsNoTracking()
            .Include(pt => pt.PaymentAttempt)
            .ThenInclude(pa => pa!.Payment)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(reviewStatus))
        {
            query = query.Where(pt => pt.ReviewStatus == reviewStatus);
        }

        if (!string.IsNullOrWhiteSpace(matchStatus))
        {
            query = query.Where(pt => pt.MatchStatus == matchStatus);
        }

        int totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(pt => pt.TransactionTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(pt => new PaymentTransactionDto(
                pt.Id,
                pt.WebhookEventId,
                pt.PaymentAttemptId,
                pt.Direction,
                pt.Amount,
                pt.AccountNumber,
                pt.Gateway,
                pt.ReferenceCode,
                pt.Content,
                pt.TransactionTime,
                pt.MatchStatus,
                pt.ReviewStatus,
                pt.PaymentAttempt != null ? pt.PaymentAttempt.PaymentCode : null,
                null,
                pt.PaymentAttempt != null ? pt.PaymentAttempt.Payment!.OrderId : null,
                pt.ResolvedBy,
                pt.ResolvedAt,
                pt.ResolutionNote
            ))
            .ToListAsync(ct);

        return Result.Success(PagedResponse<PaymentTransactionDto>.Create(items, page, pageSize, totalCount));
    }

    public async Task<Result> ResolvePaymentTransactionAsync(
        Guid transactionId,
        ResolvePaymentTransactionRequest request,
        Guid resolvedByUserId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tx = await _db.PaymentTransactions
            .Include(pt => pt.PaymentAttempt)
            .ThenInclude(pa => pa!.Payment)
            .FirstOrDefaultAsync(pt => pt.Id == transactionId, ct);

        if (tx is null)
        {
            return Result.Failure(DomainError.NotFound(ErrorCodes.NotFound, "Payment transaction was not found."));
        }

        if (tx.ReviewStatus == PaymentReviewStatusConstants.Resolved)
        {
            return Result.Failure(DomainError.Conflict("ALREADY_RESOLVED", "Transaction is already resolved."));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();

        if (string.Equals(request.Action, "Dismiss", StringComparison.OrdinalIgnoreCase))
        {
            tx.ReviewStatus = PaymentReviewStatusConstants.Resolved;
            tx.ResolvedBy = resolvedByUserId;
            tx.ResolvedAt = now;
            tx.ResolutionNote = string.IsNullOrWhiteSpace(request.Note) ? "Dismissed" : request.Note.Trim();

            _db.AuditLogs.Add(new AuditLog
            {
                Id = 0,
                ActorUserId = resolvedByUserId,
                ActorRole = "Staff",
                Action = "DismissPaymentAnomaly",
                EntityType = "PaymentTransaction",
                EntityId = tx.Id.ToString(),
                AfterJson = JsonSerializer.Serialize(new { tx.ResolutionNote }),
                At = now
            });

            await _db.SaveChangesAsync(ct);
            return Result.Success();
        }

        if (string.Equals(request.Action, "Refund", StringComparison.OrdinalIgnoreCase))
        {
            tx.ReviewStatus = PaymentReviewStatusConstants.Resolved;
            tx.ResolvedBy = resolvedByUserId;
            tx.ResolvedAt = now;
            tx.ResolutionNote = string.IsNullOrWhiteSpace(request.Note) ? "Routed to Refund" : request.Note.Trim();

            Guid orderId = request.OrderId ?? tx.PaymentAttempt?.Payment?.OrderId ?? Guid.Empty;
            Guid paymentId = tx.PaymentAttempt?.PaymentId ?? Guid.Empty;

            if (orderId != Guid.Empty && paymentId != Guid.Empty)
            {
                var refund = new Refund
                {
                    Id = Guid.NewGuid(),
                    PaymentId = paymentId,
                    OrderId = orderId,
                    Amount = tx.Amount,
                    Reason = $"Hoàn tiền từ giao dịch bất thường {tx.MatchStatus}: {tx.ResolutionNote}",
                    Status = RefundStatusConstants.Requested,
                    RequestedBy = resolvedByUserId,
                    RequestedAt = now
                };
                _db.Refunds.Add(refund);
            }

            _db.AuditLogs.Add(new AuditLog
            {
                Id = 0,
                ActorUserId = resolvedByUserId,
                ActorRole = "Staff",
                Action = "RefundPaymentAnomaly",
                EntityType = "PaymentTransaction",
                EntityId = tx.Id.ToString(),
                AfterJson = JsonSerializer.Serialize(new { tx.ResolutionNote }),
                At = now
            });

            await _db.SaveChangesAsync(ct);
            return Result.Success();
        }

        if (string.Equals(request.Action, "AttachToOrder", StringComparison.OrdinalIgnoreCase))
        {
            Guid targetOrderId = request.OrderId ?? tx.PaymentAttempt?.Payment?.OrderId ?? Guid.Empty;
            if (targetOrderId == Guid.Empty)
            {
                return Result.Failure(DomainError.Validation(ErrorCodes.ValidationFailed, "Target OrderId is required for AttachToOrder."));
            }

            var order = await _db.Orders
                .Include(o => o.Items)
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == targetOrderId, ct);

            if (order is null)
            {
                return Result.Failure(DomainError.NotFound(ErrorCodes.NotFound, "Target order was not found."));
            }

            var payment = await _db.Payments
                .FirstOrDefaultAsync(p => p.OrderId == targetOrderId, ct);

            // If order was cancelled (late payment), verify stock availability to revive
            if (string.Equals(order.Status, OrderStatusConstants.Cancelled, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var item in order.Items)
                {
                    var stock = await _db.StockItems.FirstOrDefaultAsync(s => s.ProductId == item.ProductId, ct);
                    if (stock is null || (stock.OnHand - stock.Reserved) < item.Quantity)
                    {
                        return Result.Failure(DomainError.Conflict(
                            "INSUFFICIENT_STOCK",
                            $"Sản phẩm '{item.ProductName}' không đủ tồn kho để khôi phục đơn hàng. Vui lòng chọn Hoàn tiền."));
                    }
                    stock.Reserved += item.Quantity;
                }

                order.Status = OrderStatusConstants.Confirmed;
                order.PaymentStatus = OrderPaymentStatusConstants.Paid;
                order.UpdatedAt = now;

                if (payment is not null)
                {
                    payment.Status = PaymentStatusConstants.Paid;
                    payment.PaidAt = tx.TransactionTime;
                    payment.UpdatedAt = now;
                }

                var attachHistory = new OrderStatusHistory
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    FromStatus = OrderStatusConstants.Cancelled,
                    ToStatus = OrderStatusConstants.Confirmed,
                    Reason = $"Khôi phục đơn hàng sau khi gán giao dịch chuyển tiền muộn ({tx.MatchStatus})",
                    ActorUserId = resolvedByUserId,
                    ActorRole = "Staff",
                    CreatedAt = now
                };
                _db.OrderStatusHistories.Add(attachHistory);
                order.StatusHistory.Add(attachHistory);
            }

            tx.ReviewStatus = PaymentReviewStatusConstants.Resolved;
            tx.ResolvedBy = resolvedByUserId;
            tx.ResolvedAt = now;
            tx.ResolutionNote = string.IsNullOrWhiteSpace(request.Note) ? $"Attached to order {order.OrderNumber}" : request.Note.Trim();

            _db.AuditLogs.Add(new AuditLog
            {
                Id = 0,
                ActorUserId = resolvedByUserId,
                ActorRole = "Staff",
                Action = "AttachPaymentToOrder",
                EntityType = "PaymentTransaction",
                EntityId = tx.Id.ToString(),
                AfterJson = JsonSerializer.Serialize(new { OrderId = order.Id, order.OrderNumber, tx.ResolutionNote }),
                At = now
            });

            await _db.SaveChangesAsync(ct);
            return Result.Success();
        }

        return Result.Failure(DomainError.Validation(ErrorCodes.ValidationFailed, $"Unsupported action '{request.Action}'."));
    }

    public async Task<Result<PagedResponse<RefundDto>>> GetRefundsAsync(
        string? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        var query = _db.Refunds
            .AsNoTracking()
            .Include(r => r.Order)
            .Include(r => r.Requester)
            .Include(r => r.Decider)
            .Include(r => r.Completer)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        int totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(r => r.RequestedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RefundDto(
                r.Id,
                r.PaymentId,
                r.OrderId,
                r.Order != null ? r.Order.OrderNumber : string.Empty,
                r.Amount,
                r.Reason,
                r.Status,
                r.RequestedBy,
                r.Requester != null ? r.Requester.FullName : null,
                r.RequestedAt,
                r.DecidedBy,
                r.Decider != null ? r.Decider.FullName : null,
                r.DecidedAt,
                r.CompletedBy,
                r.Completer != null ? r.Completer.FullName : null,
                r.CompletedAt,
                r.BankReference,
                r.Note
            ))
            .ToListAsync(ct);

        return Result.Success(PagedResponse<RefundDto>.Create(items, page, pageSize, totalCount));
    }

    public async Task<Result<RefundDto>> RequestRefundAsync(
        Guid orderId,
        long amount,
        string reason,
        Guid requestedByUserId,
        CancellationToken ct = default)
    {
        if (amount <= 0)
        {
            return Result.Failure<RefundDto>(DomainError.Validation(
                "REFUND_INVALID_AMOUNT", "Refund amount must be positive."));
        }

        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.OrderId == orderId, ct);

        if (order is null || payment is null)
        {
            return Result.Failure<RefundDto>(DomainError.NotFound(
                ErrorCodes.NotFound, "Order or payment was not found."));
        }

        if (!string.Equals(payment.Status, PaymentStatusConstants.Paid, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(payment.Status, PaymentStatusConstants.PartiallyRefunded, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<RefundDto>(DomainError.Conflict(
                "PAYMENT_NOT_PAID", "Only paid or partially refunded orders can be refunded."));
        }

        long alreadyRefundedOrRequested = await _db.Refunds
            .Where(r => r.PaymentId == payment.Id &&
                        (r.Status == RefundStatusConstants.Requested ||
                         r.Status == RefundStatusConstants.Approved ||
                         r.Status == RefundStatusConstants.Completed))
            .SumAsync(r => r.Amount, ct);

        if (amount > (payment.Amount - alreadyRefundedOrRequested))
        {
            return Result.Failure<RefundDto>(DomainError.Conflict(
                "REFUND_INVALID_AMOUNT",
                $"Refund amount ({amount:N0} ₫) exceeds available refundable amount ({(payment.Amount - alreadyRefundedOrRequested):N0} ₫)."));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();

        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            OrderId = order.Id,
            Amount = amount,
            Reason = reason.Trim(),
            Status = RefundStatusConstants.Requested,
            RequestedBy = requestedByUserId,
            RequestedAt = now
        };

        _db.Refunds.Add(refund);

        _db.AuditLogs.Add(new AuditLog
        {
            Id = 0,
            ActorUserId = requestedByUserId,
            ActorRole = "Staff",
            Action = "RefundRequested",
            EntityType = "Refund",
            EntityId = refund.Id.ToString(),
            AfterJson = JsonSerializer.Serialize(new { refund.OrderId, refund.Amount, refund.Reason }),
            At = now
        });

        await _db.SaveChangesAsync(ct);

        var dto = new RefundDto(
            refund.Id,
            refund.PaymentId,
            refund.OrderId,
            order.OrderNumber,
            refund.Amount,
            refund.Reason,
            refund.Status,
            refund.RequestedBy,
            null,
            refund.RequestedAt,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null
        );

        return Result.Success(dto);
    }

    public async Task<Result> ApproveRefundAsync(
        Guid refundId,
        string? note,
        Guid decidedByUserId,
        CancellationToken ct = default)
    {
        var refund = await _db.Refunds.FirstOrDefaultAsync(r => r.Id == refundId, ct);
        if (refund is null)
        {
            return Result.Failure(DomainError.NotFound(ErrorCodes.NotFound, "Refund was not found."));
        }

        if (refund.Status != RefundStatusConstants.Requested)
        {
            return Result.Failure(DomainError.Conflict("INVALID_STATUS", "Only Requested refunds can be approved."));
        }

        // Two-Person Rule (BR-15): DecidedBy cannot be the Requester
        if (refund.RequestedBy == decidedByUserId)
        {
            return Result.Failure(DomainError.Conflict(
                "REFUND_SAME_APPROVER",
                "Two-person rule violation: the approver cannot be the same person who requested the refund."));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        refund.Status = RefundStatusConstants.Approved;
        refund.DecidedBy = decidedByUserId;
        refund.DecidedAt = now;
        if (!string.IsNullOrWhiteSpace(note)) refund.Note = note.Trim();

        _db.AuditLogs.Add(new AuditLog
        {
            Id = 0,
            ActorUserId = decidedByUserId,
            ActorRole = "Manager",
            Action = "RefundApproved",
            EntityType = "Refund",
            EntityId = refund.Id.ToString(),
            AfterJson = JsonSerializer.Serialize(new { refund.Amount, refund.Note }),
            At = now
        });

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RejectRefundAsync(
        Guid refundId,
        string reason,
        Guid decidedByUserId,
        CancellationToken ct = default)
    {
        var refund = await _db.Refunds.FirstOrDefaultAsync(r => r.Id == refundId, ct);
        if (refund is null)
        {
            return Result.Failure(DomainError.NotFound(ErrorCodes.NotFound, "Refund was not found."));
        }

        if (refund.Status != RefundStatusConstants.Requested)
        {
            return Result.Failure(DomainError.Conflict("INVALID_STATUS", "Only Requested refunds can be rejected."));
        }

        // Two-Person Rule (BR-15): DecidedBy cannot be the Requester
        if (refund.RequestedBy == decidedByUserId)
        {
            return Result.Failure(DomainError.Conflict(
                "REFUND_SAME_APPROVER",
                "Two-person rule violation: the decider cannot be the same person who requested the refund."));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        refund.Status = RefundStatusConstants.Rejected;
        refund.DecidedBy = decidedByUserId;
        refund.DecidedAt = now;
        refund.Note = reason.Trim();

        _db.AuditLogs.Add(new AuditLog
        {
            Id = 0,
            ActorUserId = decidedByUserId,
            ActorRole = "Manager",
            Action = "RefundRejected",
            EntityType = "Refund",
            EntityId = refund.Id.ToString(),
            AfterJson = JsonSerializer.Serialize(new { refund.Amount, refund.Note }),
            At = now
        });

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> CompleteRefundAsync(
        Guid refundId,
        string bankReference,
        string? note,
        Guid completedByUserId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(bankReference))
        {
            return Result.Failure(DomainError.Validation("BANK_REFERENCE_REQUIRED", "Bank payout reference code is required."));
        }

        var refund = await _db.Refunds
            .Include(r => r.Payment)
            .Include(r => r.Order)
            .FirstOrDefaultAsync(r => r.Id == refundId, ct);

        if (refund is null || refund.Payment is null || refund.Order is null)
        {
            return Result.Failure(DomainError.NotFound(ErrorCodes.NotFound, "Refund or payment was not found."));
        }

        if (refund.Status != RefundStatusConstants.Approved)
        {
            return Result.Failure(DomainError.Conflict("INVALID_STATUS", "Only Approved refunds can be marked Completed."));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        refund.Status = RefundStatusConstants.Completed;
        refund.BankReference = bankReference.Trim();
        refund.CompletedBy = completedByUserId;
        refund.CompletedAt = now;
        if (!string.IsNullOrWhiteSpace(note)) refund.Note = note.Trim();

        long totalCompletedRefunds = await _db.Refunds
            .Where(r => r.PaymentId == refund.PaymentId && r.Status == RefundStatusConstants.Completed && r.Id != refund.Id)
            .SumAsync(r => r.Amount, ct) + refund.Amount;

        bool fullyRefunded = totalCompletedRefunds >= refund.Payment.Amount;
        string newPaymentStatus = fullyRefunded ? PaymentStatusConstants.Refunded : PaymentStatusConstants.PartiallyRefunded;
        string newOrderPaymentStatus = fullyRefunded ? OrderPaymentStatusConstants.Refunded : OrderPaymentStatusConstants.PartiallyRefunded;

        refund.Payment.Status = newPaymentStatus;
        refund.Payment.UpdatedAt = now;

        refund.Order.PaymentStatus = newOrderPaymentStatus;
        refund.Order.UpdatedAt = now;

        _db.AuditLogs.Add(new AuditLog
        {
            Id = 0,
            ActorUserId = completedByUserId,
            ActorRole = "Manager",
            Action = "RefundCompleted",
            EntityType = "Refund",
            EntityId = refund.Id.ToString(),
            AfterJson = JsonSerializer.Serialize(new { refund.Amount, refund.BankReference, newPaymentStatus }),
            At = now
        });

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<string>> SimulateWebhookPayloadAsync(
        SimulateWebhookRequest request,
        CancellationToken ct = default)
    {
        string code = request.PaymentCode ?? "GH1000000123";
        long amount = request.TransferAmount ?? 190000L;
        string type = request.TransferType ?? "in";
        string account = request.AccountNumber ?? _options.AccountNumber;
        string gateway = request.Gateway ?? _options.BankShortName;
        string content = request.Content ?? $"{gateway} {code} thanh toan don hang";

        DateTimeOffset now = _timeProvider.GetUtcNow();
        long id = Random.Shared.Next(100000, 999999);

        var payload = new SePayWebhookPayloadDto(
            id,
            gateway,
            now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            account,
            _options.SubAccount,
            code,
            content,
            type,
            content,
            amount,
            15000000L,
            $"FT{Random.Shared.Next(10000000, 99999999)}"
        );

        string json = JsonSerializer.Serialize(payload);
        return await Task.FromResult(Result.Success(json));
    }

    private async Task<long> GetNextSequenceValueAsync(string sequenceName, CancellationToken ct)
    {
        var connection = _db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT NEXT VALUE FOR {sequenceName}";
        var currentTx = _db.Database.CurrentTransaction?.GetDbTransaction();
        if (currentTx is not null)
        {
            command.Transaction = currentTx;
        }
        var result = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }
}

