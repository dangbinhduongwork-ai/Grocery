using GroceryHub.SharedKernel;

namespace GroceryHub.Persistence.Entities;

public static class PaymentMethodConstants
{
    public const string COD = "COD";
    public const string SepayQr = "SEPAY_QR";

    public static readonly IReadOnlyList<string> All =
    [
        COD,
        SepayQr
    ];
}

public static class PaymentStatusConstants
{
    public const string Unpaid = "Unpaid";
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Failed = "Failed";
    public const string Expired = "Expired";
    public const string PartiallyRefunded = "PartiallyRefunded";
    public const string Refunded = "Refunded";

    public static readonly IReadOnlyList<string> All =
    [
        Unpaid,
        Pending,
        Paid,
        Failed,
        Expired,
        PartiallyRefunded,
        Refunded
    ];
}

public class Payment : IAuditableEntity
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string PaymentCode { get; set; } = string.Empty;

    public string Method { get; set; } = PaymentMethodConstants.COD;

    public string Status { get; set; } = PaymentStatusConstants.Unpaid;

    public long Amount { get; set; }

    public DateTimeOffset? PaidAt { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<PaymentAttempt> Attempts { get; set; } = new List<PaymentAttempt>();

    public ICollection<CodCollection> CodCollections { get; set; } = new List<CodCollection>();
}
