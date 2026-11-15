using GroceryHub.SharedKernel;

namespace GroceryHub.Persistence.Entities;

public static class OrderStatusConstants
{
    public const string PendingPayment = "PendingPayment";
    public const string Placed = "Placed";
    public const string Confirmed = "Confirmed";
    public const string Preparing = "Preparing";
    public const string Packed = "Packed";
    public const string Shipping = "Shipping";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";
    public const string DeliveryFailed = "DeliveryFailed";

    public static readonly IReadOnlyList<string> All =
    [
        PendingPayment,
        Placed,
        Confirmed,
        Preparing,
        Packed,
        Shipping,
        Delivered,
        Cancelled,
        DeliveryFailed
    ];
}

public static class OrderPaymentMethodConstants
{
    public const string COD = "COD";
    public const string SepayQr = "SEPAY_QR";

    public static readonly IReadOnlyList<string> All =
    [
        COD,
        SepayQr
    ];
}

public static class OrderPaymentStatusConstants
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

public class Order : IAuditableEntity
{
    public Guid Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public string Status { get; set; } = OrderStatusConstants.PendingPayment;

    public long Subtotal { get; set; }

    public long CouponDiscount { get; set; } = 0;

    public long ShippingFee { get; set; } = 0;

    public long Total { get; set; }

    public string PaymentMethod { get; set; } = OrderPaymentMethodConstants.COD;

    public string PaymentStatus { get; set; } = OrderPaymentStatusConstants.Unpaid;

    public string ShippingRecipientName { get; set; } = string.Empty;

    public string ShippingPhone { get; set; } = string.Empty;

    public string ShippingProvinceCode { get; set; } = string.Empty;

    public string ShippingWardCode { get; set; } = string.Empty;

    public string ShippingProvinceName { get; set; } = string.Empty;

    public string ShippingWardName { get; set; } = string.Empty;

    public string ShippingStreet { get; set; } = string.Empty;

    public string? ShippingNote { get; set; }

    public string? CancellationReason { get; set; }

    public Guid? CancelledBy { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public User? User { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
}
