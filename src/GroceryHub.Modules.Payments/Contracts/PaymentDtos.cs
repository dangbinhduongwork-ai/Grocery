using System.Text.Json.Serialization;

namespace GroceryHub.Modules.Payments.Contracts;

public sealed record SePayWebhookPayloadDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("gateway")] string Gateway,
    [property: JsonPropertyName("transactionDate")] string TransactionDate,
    [property: JsonPropertyName("accountNumber")] string AccountNumber,
    [property: JsonPropertyName("subAccount")] string? SubAccount,
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("transferType")] string TransferType,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("transferAmount")] long TransferAmount,
    [property: JsonPropertyName("accumulated")] long Accumulated,
    [property: JsonPropertyName("referenceCode")] string? ReferenceCode
);

public sealed record QrPaymentDetailsDto(
    string Url,
    string Bank,
    string AccountNumber,
    string AccountName,
    long Amount,
    string Content,
    DateTimeOffset ExpiresAt
);

public sealed record PaymentAttemptDto(
    Guid Id,
    Guid PaymentId,
    string PaymentCode,
    int AttemptNumber,
    long Amount,
    string Status,
    DateTimeOffset ExpiresAt,
    QrPaymentDetailsDto Qr
);

public sealed record PaymentStatusResponseDto(
    string PaymentCode,
    string Status,
    string Method,
    long Amount,
    DateTimeOffset? PaidAt,
    string OrderStatus,
    DateTimeOffset? ExpiresAt,
    bool CanRetry,
    int AttemptNumber,
    int MaxAttempts,
    Guid? OrderId = null,
    string? OrderNumber = null,
    PaymentAttemptDto? Attempt = null,
    QrPaymentDetailsDto? Qr = null
);

public sealed record PaymentTransactionDto(
    Guid Id,
    Guid WebhookEventId,
    Guid? PaymentAttemptId,
    string Direction,
    long Amount,
    string AccountNumber,
    string Gateway,
    string? ReferenceCode,
    string Content,
    DateTimeOffset TransactionTime,
    string MatchStatus,
    string ReviewStatus,
    string? PaymentCode,
    string? OrderNumber,
    Guid? OrderId,
    Guid? ResolvedBy,
    DateTimeOffset? ResolvedAt,
    string? ResolutionNote
);

public sealed record ResolvePaymentTransactionRequest(
    string Action,
    Guid? OrderId = null,
    string? Note = null
);

public sealed record RefundDto(
    Guid Id,
    Guid PaymentId,
    Guid OrderId,
    string OrderNumber,
    long Amount,
    string Reason,
    string Status,
    Guid RequestedBy,
    string? RequestedByName,
    DateTimeOffset RequestedAt,
    Guid? DecidedBy,
    string? DecidedByName,
    DateTimeOffset? DecidedAt,
    Guid? CompletedBy,
    string? CompletedByName,
    DateTimeOffset? CompletedAt,
    string? BankReference,
    string? Note
);

public sealed record CreateRefundRequest(
    Guid OrderId,
    long Amount,
    string Reason
);

public sealed record ApproveRefundRequest(
    string? Note = null
);

public sealed record RejectRefundRequest(
    string Reason
);

public sealed record CompleteRefundRequest(
    string BankReference,
    string? Note = null
);

public sealed record SimulateWebhookRequest(
    string? PaymentCode = null,
    long? TransferAmount = null,
    string? TransferType = "in",
    string? AccountNumber = null,
    string? Gateway = "MBBank",
    string? Content = null
);
