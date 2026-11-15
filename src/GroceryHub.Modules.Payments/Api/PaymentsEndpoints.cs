using System.Security.Claims;
using System.Text;
using GroceryHub.Modules.Payments.Application;
using GroceryHub.Modules.Payments.Contracts;
using GroceryHub.SharedKernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace GroceryHub.Modules.Payments.Api;

public static class PaymentsEndpoints
{
    private const string PolicyAuth = "auth";
    private const string PolicyPaymentsReview = "payments.review";
    private const string PolicyRefundsRequest = "refunds.request";
    private const string PolicyRefundsApprove = "refunds.approve";

    public static IEndpointRouteBuilder MapPaymentsApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("").WithTags("Payments");

        // 1. SePay Webhook Endpoint (Anonymous, max 64KB body)
        group.MapPost("/payments/sepay/webhook", async (
            HttpRequest request,
            IPaymentService paymentService,
            CancellationToken ct) =>
        {
            const int maxBodyLength = 65536; // 64 KB

            if (request.ContentLength.HasValue && request.ContentLength.Value > maxBodyLength)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status413PayloadTooLarge,
                    title: "Payload Too Large",
                    detail: "Webhook payload exceeds 64KB limit.");
            }

            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var buffer = new char[maxBodyLength + 1];
            int read = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
            if (read > maxBodyLength)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status413PayloadTooLarge,
                    title: "Payload Too Large",
                    detail: "Webhook payload exceeds 64KB limit.");
            }

            string rawBody = new(buffer, 0, read);

            request.Headers.TryGetValue("X-Sepay-Timestamp", out var tsVal);
            request.Headers.TryGetValue("X-Sepay-Signature", out var sigVal);
            request.Headers.TryGetValue("Authorization", out var authVal);

            string? clientIp = request.HttpContext.Connection.RemoteIpAddress?.ToString();

            var (statusCode, success, error) = await paymentService.ProcessWebhookAsync(
                rawBody,
                tsVal.FirstOrDefault(),
                sigVal.FirstOrDefault(),
                authVal.FirstOrDefault(),
                clientIp,
                ct);

            if (success)
            {
                return Results.Ok(new { success = true });
            }

            return Results.Json(new { success = false, error }, statusCode: statusCode);
        })
        .AllowAnonymous()
        .WithName("ProcessSePayWebhook")
        .WithSummary("Receive SePay VietQR payment webhook notifications");

        // 2. Read-only payment status by code (Owner or Staff only)
        group.MapGet("/payments/{paymentCode}/status", async (
            string paymentCode,
            ClaimsPrincipal user,
            IPaymentService paymentService,
            CancellationToken ct) =>
        {
            Guid? userId = GetUserIdFromClaims(user);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            bool isStaff = IsStaffUser(user);

            var result = await paymentService.GetPaymentStatusByCodeAsync(paymentCode, userId, isStaff, ct);
            if (result.IsSuccess)
            {
                return Results.Ok(result.Value);
            }

            return ToProblemResult(result.Error);
        })
        .RequireAuthorization(PolicyAuth)
        .WithName("GetPaymentStatusByCode")
        .WithSummary("Get read-only payment attempt and order status by payment code");

        // 3. Customer generates new payment attempt (up to 5 max)
        group.MapPost("/me/orders/{id:guid}/payment-attempts", async (
            Guid id,
            ClaimsPrincipal user,
            HttpRequest request,
            IPaymentService paymentService,
            CancellationToken ct) =>
        {
            Guid? userId = GetUserIdFromClaims(user);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            request.Headers.TryGetValue("Idempotency-Key", out var ikVal);

            var result = await paymentService.CreateNewPaymentAttemptAsync(id, userId.Value, ikVal.FirstOrDefault(), ct);
            if (result.IsSuccess)
            {
                return Results.Created($"/api/v1/payments/{result.Value.PaymentCode}/status", result.Value);
            }

            return ToProblemResult(result.Error);
        })
        .RequireAuthorization(PolicyAuth)
        .WithName("CreateNewPaymentAttempt")
        .WithSummary("Generate a new VietQR payment attempt for an order in PendingPayment status");

        // 4. Admin Payment Transactions Queue
        group.MapGet("/admin/payment-transactions", async (
            [FromQuery] string? reviewStatus,
            [FromQuery] string? matchStatus,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            IPaymentService paymentService,
            CancellationToken ct) =>
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 20;

            var result = await paymentService.GetPaymentTransactionsAsync(reviewStatus, matchStatus, page, pageSize, ct);
            if (result.IsSuccess)
            {
                return Results.Ok(result.Value);
            }

            return ToProblemResult(result.Error);
        })
        .RequireAuthorization(PolicyPaymentsReview)
        .WithName("GetPaymentTransactions")
        .WithSummary("List all webhook payment transactions with optional filter by review and match status");

        // 5. Admin Resolve Payment Transaction
        group.MapPost("/admin/payment-transactions/{id:guid}/resolve", async (
            Guid id,
            [FromBody] ResolvePaymentTransactionRequest req,
            ClaimsPrincipal user,
            IPaymentService paymentService,
            CancellationToken ct) =>
        {
            Guid? staffId = GetUserIdFromClaims(user);
            if (staffId is null)
            {
                return Results.Unauthorized();
            }

            var result = await paymentService.ResolvePaymentTransactionAsync(id, req, staffId.Value, ct);
            if (result.IsSuccess)
            {
                return Results.Ok(new { success = true });
            }

            return ToProblemResult(result.Error);
        })
        .RequireAuthorization(PolicyPaymentsReview)
        .WithName("ResolvePaymentTransaction")
        .WithSummary("Resolve a payment transaction anomaly (AttachToOrder, Refund, Dismiss)");

        // 6. Admin Refunds Queue
        group.MapGet("/admin/refunds", async (
            [FromQuery] string? status,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            IPaymentService paymentService,
            CancellationToken ct) =>
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 20;

            var result = await paymentService.GetRefundsAsync(status, page, pageSize, ct);
            if (result.IsSuccess)
            {
                return Results.Ok(result.Value);
            }

            return ToProblemResult(result.Error);
        })
        .RequireAuthorization(PolicyRefundsRequest)
        .WithName("GetRefunds")
        .WithSummary("List all refunds with optional status filter");

        // 7. Admin Request Refund
        group.MapPost("/admin/refunds", async (
            [FromBody] CreateRefundRequest req,
            ClaimsPrincipal user,
            IPaymentService paymentService,
            CancellationToken ct) =>
        {
            Guid? staffId = GetUserIdFromClaims(user);
            if (staffId is null)
            {
                return Results.Unauthorized();
            }

            var result = await paymentService.RequestRefundAsync(req.OrderId, req.Amount, req.Reason, staffId.Value, ct);
            if (result.IsSuccess)
            {
                return Results.Created($"/api/v1/admin/refunds/{result.Value.Id}", result.Value);
            }

            return ToProblemResult(result.Error);
        })
        .RequireAuthorization(PolicyRefundsRequest)
        .WithName("RequestRefund")
        .WithSummary("Request a refund for a paid or partially refunded order");

        // 8. Admin Approve Refund (Two-person rule)
        group.MapPost("/admin/refunds/{id:guid}/approve", async (
            Guid id,
            [FromBody] ApproveRefundRequest? req,
            ClaimsPrincipal user,
            IPaymentService paymentService,
            CancellationToken ct) =>
        {
            Guid? staffId = GetUserIdFromClaims(user);
            if (staffId is null)
            {
                return Results.Unauthorized();
            }

            var result = await paymentService.ApproveRefundAsync(id, req?.Note, staffId.Value, ct);
            if (result.IsSuccess)
            {
                return Results.Ok(new { success = true });
            }

            return ToProblemResult(result.Error);
        })
        .RequireAuthorization(PolicyRefundsApprove)
        .WithName("ApproveRefund")
        .WithSummary("Approve a requested refund (must not be the same person who requested)");

        // 9. Admin Reject Refund
        group.MapPost("/admin/refunds/{id:guid}/reject", async (
            Guid id,
            [FromBody] RejectRefundRequest req,
            ClaimsPrincipal user,
            IPaymentService paymentService,
            CancellationToken ct) =>
        {
            Guid? staffId = GetUserIdFromClaims(user);
            if (staffId is null)
            {
                return Results.Unauthorized();
            }

            var result = await paymentService.RejectRefundAsync(id, req.Reason, staffId.Value, ct);
            if (result.IsSuccess)
            {
                return Results.Ok(new { success = true });
            }

            return ToProblemResult(result.Error);
        })
        .RequireAuthorization(PolicyRefundsApprove)
        .WithName("RejectRefund")
        .WithSummary("Reject a requested refund");

        // 10. Admin Complete Refund
        group.MapPost("/admin/refunds/{id:guid}/complete", async (
            Guid id,
            [FromBody] CompleteRefundRequest req,
            ClaimsPrincipal user,
            IPaymentService paymentService,
            CancellationToken ct) =>
        {
            Guid? staffId = GetUserIdFromClaims(user);
            if (staffId is null)
            {
                return Results.Unauthorized();
            }

            var result = await paymentService.CompleteRefundAsync(id, req.BankReference, req.Note, staffId.Value, ct);
            if (result.IsSuccess)
            {
                return Results.Ok(new { success = true });
            }

            return ToProblemResult(result.Error);
        })
        .RequireAuthorization(PolicyRefundsApprove)
        .WithName("CompleteRefund")
        .WithSummary("Mark an approved refund as completed with bank transfer reference");

        // 11. Dev Webhook Simulator (Development only - returns 404 in non-Development environments)
        endpoints.MapPost("/dev/sepay/simulate", async (
            [FromBody] SimulateWebhookRequest req,
            IWebHostEnvironment env,
            IPaymentService paymentService,
            CancellationToken ct) =>
        {
            if (!env.IsDevelopment())
            {
                return Results.NotFound();
            }

            var result = await paymentService.SimulateWebhookPayloadAsync(req, ct);
            if (result.IsSuccess)
            {
                return Results.Ok(new { payload = result.Value });
            }

            return ToProblemResult(result.Error);
        })
        .AllowAnonymous()
        .WithName("SimulateSePayWebhook")
        .WithSummary("Development only: simulate a signed SePay webhook payload");

        return endpoints;
    }

    private static Guid? GetUserIdFromClaims(ClaimsPrincipal user)
    {
        string? sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        return Guid.TryParse(sub, out var guid) ? guid : null;
    }

    private static bool IsStaffUser(ClaimsPrincipal user)
    {
        return user.IsInRole("Admin") ||
               user.IsInRole("Manager") ||
               user.IsInRole("Staff") ||
               user.HasClaim("permission", "payments.review") ||
               user.HasClaim("permission", "payments.read") ||
               user.HasClaim("permission", "orders.read");
    }

    private static IResult ToProblemResult(DomainError error)
    {
        int statusCode = (int)error.Type;

        return Results.Problem(
            statusCode: statusCode,
            title: error.Code,
            detail: error.Message
        );
    }
}
