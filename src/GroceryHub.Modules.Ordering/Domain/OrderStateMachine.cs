using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;

namespace GroceryHub.Modules.Ordering.Domain;

public static class OrderStateMachine
{
    public static bool CanCustomerCancel(string status)
    {
        return string.Equals(status, OrderStatusConstants.PendingPayment, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(status, OrderStatusConstants.Placed, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(status, OrderStatusConstants.Confirmed, StringComparison.OrdinalIgnoreCase);
    }

    public static Result ValidateStaffTransition(
        string currentStatus,
        string targetStatus,
        string paymentStatus,
        IReadOnlyCollection<string> callerPermissions,
        string? cancelReason,
        long? codAmount,
        long expectedTotal)
    {
        // Cancel transitions
        if (string.Equals(targetStatus, OrderStatusConstants.Cancelled, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(cancelReason))
            {
                return Result.Failure(DomainError.Validation(
                    ErrorCodes.ValidationFailed,
                    "Cancellation reason is required."));
            }

            bool isPaid = string.Equals(paymentStatus, OrderPaymentStatusConstants.Paid, StringComparison.OrdinalIgnoreCase);
            bool isEarlyState = string.Equals(currentStatus, OrderStatusConstants.PendingPayment, StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(currentStatus, OrderStatusConstants.Placed, StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(currentStatus, OrderStatusConstants.Confirmed, StringComparison.OrdinalIgnoreCase);

            if (isEarlyState && !isPaid)
            {
                // Unpaid early orders can be cancelled with orders.process or orders.cancel
                if (!callerPermissions.Contains("orders.process") && !callerPermissions.Contains("orders.cancel"))
                {
                    return Result.Failure(DomainError.Forbidden(
                        ErrorCodes.Forbidden,
                        "Permission 'orders.process' or 'orders.cancel' is required to cancel this order."));
                }

                return Result.Success();
            }

            // Preparing, Packed, DeliveryFailed, or ANY Paid order requires orders.cancel (DEC-011 / Q-006)
            bool isMidState = string.Equals(currentStatus, OrderStatusConstants.Preparing, StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(currentStatus, OrderStatusConstants.Packed, StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(currentStatus, OrderStatusConstants.DeliveryFailed, StringComparison.OrdinalIgnoreCase);

            if (isMidState || isPaid)
            {
                if (!callerPermissions.Contains("orders.cancel"))
                {
                    return Result.Failure(DomainError.Forbidden(
                        ErrorCodes.Forbidden,
                        "Permission 'orders.cancel' is required to cancel this order."));
                }

                return Result.Success();
            }

            return Result.Failure(DomainError.Conflict(
                ErrorCodes.OrderInvalidTransition,
                $"Cannot cancel order from current state '{currentStatus}'."));
        }

        // Forward and operational transitions
        if (string.Equals(currentStatus, OrderStatusConstants.Placed, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(targetStatus, OrderStatusConstants.Confirmed, StringComparison.OrdinalIgnoreCase))
        {
            if (!callerPermissions.Contains("orders.process"))
            {
                return Result.Failure(DomainError.Forbidden(ErrorCodes.Forbidden, "Permission 'orders.process' required."));
            }
            return Result.Success();
        }

        if (string.Equals(currentStatus, OrderStatusConstants.Confirmed, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(targetStatus, OrderStatusConstants.Preparing, StringComparison.OrdinalIgnoreCase))
        {
            if (!callerPermissions.Contains("orders.process"))
            {
                return Result.Failure(DomainError.Forbidden(ErrorCodes.Forbidden, "Permission 'orders.process' required."));
            }
            return Result.Success();
        }

        if (string.Equals(currentStatus, OrderStatusConstants.Preparing, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(targetStatus, OrderStatusConstants.Packed, StringComparison.OrdinalIgnoreCase))
        {
            if (!callerPermissions.Contains("orders.process"))
            {
                return Result.Failure(DomainError.Forbidden(ErrorCodes.Forbidden, "Permission 'orders.process' required."));
            }
            return Result.Success();
        }

        if (string.Equals(currentStatus, OrderStatusConstants.Packed, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(targetStatus, OrderStatusConstants.Shipping, StringComparison.OrdinalIgnoreCase))
        {
            if (!callerPermissions.Contains("orders.ship"))
            {
                return Result.Failure(DomainError.Forbidden(ErrorCodes.Forbidden, "Permission 'orders.ship' required."));
            }
            return Result.Success();
        }

        if (string.Equals(currentStatus, OrderStatusConstants.Shipping, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(targetStatus, OrderStatusConstants.Delivered, StringComparison.OrdinalIgnoreCase))
        {
            if (!callerPermissions.Contains("orders.ship"))
            {
                return Result.Failure(DomainError.Forbidden(ErrorCodes.Forbidden, "Permission 'orders.ship' required."));
            }

            if (codAmount.HasValue && codAmount.Value != expectedTotal)
            {
                return Result.Failure(DomainError.Validation(
                    ErrorCodes.CodAmountMismatch,
                    $"Collected COD amount ({codAmount.Value:N0} ₫) does not equal expected order total ({expectedTotal:N0} ₫)."));
            }

            return Result.Success();
        }

        if (string.Equals(currentStatus, OrderStatusConstants.Shipping, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(targetStatus, OrderStatusConstants.DeliveryFailed, StringComparison.OrdinalIgnoreCase))
        {
            if (!callerPermissions.Contains("orders.ship"))
            {
                return Result.Failure(DomainError.Forbidden(ErrorCodes.Forbidden, "Permission 'orders.ship' required."));
            }

            if (string.IsNullOrWhiteSpace(cancelReason))
            {
                return Result.Failure(DomainError.Validation(
                    ErrorCodes.ValidationFailed,
                    "Failure reason is required for DeliveryFailed."));
            }

            return Result.Success();
        }

        if (string.Equals(currentStatus, OrderStatusConstants.DeliveryFailed, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(targetStatus, OrderStatusConstants.Shipping, StringComparison.OrdinalIgnoreCase))
        {
            if (!callerPermissions.Contains("orders.ship"))
            {
                return Result.Failure(DomainError.Forbidden(ErrorCodes.Forbidden, "Permission 'orders.ship' required."));
            }
            return Result.Success();
        }

        return Result.Failure(DomainError.Conflict(
            ErrorCodes.OrderInvalidTransition,
            $"Invalid order state transition from '{currentStatus}' to '{targetStatus}'."));
    }
}
