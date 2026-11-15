namespace GroceryHub.SharedKernel;

public enum ErrorType
{
    BadRequest = 400,
    Unauthorized = 401,
    Forbidden = 403,
    NotFound = 404,
    Conflict = 409,
    Concurrency = 412,
    PreconditionFailed = Concurrency,
    Validation = 422,
    RateLimited = 429,
    TooManyRequests = RateLimited,
    Unexpected = 500
}

public sealed record DomainError(
    string Code,
    string Message,
    ErrorType Type = ErrorType.Conflict,
    IReadOnlyDictionary<string, string[]>? FieldErrors = null)
{
    public static readonly DomainError None = new(string.Empty, string.Empty, ErrorType.Validation);

    public static DomainError Validation(
        string code,
        string message,
        IReadOnlyDictionary<string, string[]>? fieldErrors = null) =>
        new(code, message, ErrorType.Validation, fieldErrors);

    public static DomainError ValidationFailed(
        IReadOnlyDictionary<string, string[]> fieldErrors,
        string message = "One or more validation errors occurred.") =>
        new(ErrorCodes.ValidationFailed, message, ErrorType.Validation, fieldErrors);

    public static DomainError BadRequest(string code, string message) =>
        new(code, message, ErrorType.BadRequest);

    public static DomainError Unauthorized(string code = ErrorCodes.Unauthenticated, string message = "Authentication is required.") =>
        new(code, message, ErrorType.Unauthorized);

    public static DomainError Forbidden(string code = ErrorCodes.Forbidden, string message = "You do not have permission to perform this action.") =>
        new(code, message, ErrorType.Forbidden);

    public static DomainError NotFound(string message = "The requested resource was not found.", string code = ErrorCodes.NotFound) =>
        new(code, message, ErrorType.NotFound);

    public static DomainError Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    public static DomainError Concurrency(string message = "The resource was modified by another request.", string code = ErrorCodes.ConcurrencyConflict) =>
        new(code, message, ErrorType.Concurrency);

    public static DomainError RateLimited(string message = "Too many requests. Please try again later.", string code = ErrorCodes.RateLimited) =>
        new(code, message, ErrorType.RateLimited);

    public static DomainError Unexpected(string message = "An unexpected error occurred.", string code = ErrorCodes.InternalError) =>
        new(code, message, ErrorType.Unexpected);
}

public static class ErrorCodes
{
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string BadRequest = "BAD_REQUEST";
    public const string Unauthenticated = "UNAUTHENTICATED";
    public const string Forbidden = "FORBIDDEN";
    public const string NotFound = "NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string RateLimited = "RATE_LIMITED";
    public const string IdempotencyKeyReused = "IDEMPOTENCY_KEY_REUSED";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
    public const string EmailAlreadyRegistered = "EMAIL_ALREADY_REGISTERED";
    public const string AccountLocked = "ACCOUNT_LOCKED";
    public const string EmailNotVerified = "EMAIL_NOT_VERIFIED";
    public const string RefreshTokenInvalid = "REFRESH_TOKEN_INVALID";
    public const string ProductUnavailable = "PRODUCT_UNAVAILABLE";
    public const string InsufficientStock = "INSUFFICIENT_STOCK";
    public const string QuantityLimitExceeded = "QUANTITY_LIMIT_EXCEEDED";
    public const string CartEmpty = "CART_EMPTY";
    public const string CouponInvalid = "COUPON_INVALID";
    public const string CouponExpired = "COUPON_EXPIRED";
    public const string CouponMinSubtotal = "COUPON_MIN_SUBTOTAL";
    public const string CouponLimitReached = "COUPON_LIMIT_REACHED";
    public const string AddressLimitReached = "ADDRESS_LIMIT_REACHED";
    public const string AddressNotServiceable = "ADDRESS_NOT_SERVICEABLE";
    public const string CodLimitExceeded = "COD_LIMIT_EXCEEDED";
    public const string OrderInvalidTransition = "ORDER_INVALID_TRANSITION";
    public const string OrderNotCancellable = "ORDER_NOT_CANCELLABLE";
    public const string PaymentMethodUnavailable = "PAYMENT_METHOD_UNAVAILABLE";
    public const string PaymentExpired = "PAYMENT_EXPIRED";
    public const string PaymentAlreadyPaid = "PAYMENT_ALREADY_PAID";
    public const string CodAmountMismatch = "COD_AMOUNT_MISMATCH";
    public const string RefundInvalidAmount = "REFUND_INVALID_AMOUNT";
    public const string RefundSameApprover = "REFUND_SAME_APPROVER";
    public const string ReviewNotEligible = "REVIEW_NOT_ELIGIBLE";
    public const string ReviewAlreadyExists = "REVIEW_ALREADY_EXISTS";
    public const string PoInvalidState = "PO_INVALID_STATE";
    public const string FileTypeNotAllowed = "FILE_TYPE_NOT_ALLOWED";
    public const string FileTooLarge = "FILE_TOO_LARGE";
    public const string InternalError = "INTERNAL_ERROR";
}
