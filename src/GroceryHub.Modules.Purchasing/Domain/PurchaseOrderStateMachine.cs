using GroceryHub.SharedKernel;

namespace GroceryHub.Modules.Purchasing.Domain;

public static class PurchaseOrderStates
{
    public const string Draft = "Draft";
    public const string Submitted = "Submitted";
    public const string Ordered = "Ordered";
    public const string PartiallyReceived = "PartiallyReceived";
    public const string Received = "Received";
    public const string Closed = "Closed";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyList<string> All =
    [
        Draft,
        Submitted,
        Ordered,
        PartiallyReceived,
        Received,
        Closed,
        Cancelled
    ];

    public static bool IsSubmittedOrOrdered(string? state) =>
        string.Equals(state, Submitted, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(state, Ordered, StringComparison.OrdinalIgnoreCase);
}

public static class PurchaseOrderStateMachine
{
    public static bool CanTransition(string currentStatus, string targetStatus, int totalReceivedQuantity = 0)
    {
        string from = currentStatus?.Trim() ?? string.Empty;
        string to = targetStatus?.Trim() ?? string.Empty;

        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(from, PurchaseOrderStates.PartiallyReceived, StringComparison.OrdinalIgnoreCase))
        {
            return true; // Additional goods receipt
        }

        if (string.Equals(from, PurchaseOrderStates.Draft, StringComparison.OrdinalIgnoreCase))
        {
            return PurchaseOrderStates.IsSubmittedOrOrdered(to) ||
                   string.Equals(to, PurchaseOrderStates.Cancelled, StringComparison.OrdinalIgnoreCase);
        }

        if (PurchaseOrderStates.IsSubmittedOrOrdered(from))
        {
            if (string.Equals(to, PurchaseOrderStates.Cancelled, StringComparison.OrdinalIgnoreCase))
            {
                return totalReceivedQuantity == 0;
            }

            return string.Equals(to, PurchaseOrderStates.PartiallyReceived, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(to, PurchaseOrderStates.Received, StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(from, PurchaseOrderStates.PartiallyReceived, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(to, PurchaseOrderStates.PartiallyReceived, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(to, PurchaseOrderStates.Received, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(to, PurchaseOrderStates.Closed, StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(from, PurchaseOrderStates.Received, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(to, PurchaseOrderStates.Closed, StringComparison.OrdinalIgnoreCase);
        }

        // Closed and Cancelled are terminal
        return false;
    }

    public static Result ValidateTransition(string currentStatus, string targetStatus, int totalReceivedQuantity = 0)
    {
        if (CanTransition(currentStatus, targetStatus, totalReceivedQuantity))
        {
            return Result.Success();
        }

        if (PurchaseOrderStates.IsSubmittedOrOrdered(currentStatus) &&
            string.Equals(targetStatus, PurchaseOrderStates.Cancelled, StringComparison.OrdinalIgnoreCase) &&
            totalReceivedQuantity > 0)
        {
            return Result.Failure(DomainError.Conflict(
                ErrorCodes.PoInvalidState,
                $"Cannot cancel purchase order '{currentStatus}' because {totalReceivedQuantity} units have already been received."));
        }

        return Result.Failure(DomainError.Conflict(
            ErrorCodes.PoInvalidState,
            $"Transition from purchase order status '{currentStatus}' to '{targetStatus}' is invalid."));
    }
}
