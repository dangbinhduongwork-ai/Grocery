using GroceryHub.Modules.Ordering.Domain;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Xunit;

namespace GroceryHub.UnitTests.Ordering;

public sealed class OrderStateMachineTests
{
    [Theory]
    [InlineData(OrderStatusConstants.PendingPayment, true)]
    [InlineData(OrderStatusConstants.Placed, true)]
    [InlineData(OrderStatusConstants.Confirmed, true)]
    [InlineData(OrderStatusConstants.Preparing, false)]
    [InlineData(OrderStatusConstants.Packed, false)]
    [InlineData(OrderStatusConstants.Shipping, false)]
    [InlineData(OrderStatusConstants.Delivered, false)]
    [InlineData(OrderStatusConstants.DeliveryFailed, false)]
    [InlineData(OrderStatusConstants.Cancelled, false)]
    public void CanCustomerCancel_FollowsBusinessRuleBR07(string status, bool expectedCanCancel)
    {
        bool result = OrderStateMachine.CanCustomerCancel(status);
        Assert.Equal(expectedCanCancel, result);
    }

    [Fact]
    public void StaffTransition_PlacedToConfirmed_RequiresOrdersProcessPermission()
    {
        var withoutPerm = OrderStateMachine.ValidateStaffTransition(
            OrderStatusConstants.Placed,
            OrderStatusConstants.Confirmed,
            OrderPaymentStatusConstants.Unpaid,
            callerPermissions: ["inventory.read"],
            cancelReason: null,
            codAmount: null,
            expectedTotal: 100_000);

        Assert.False(withoutPerm.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, withoutPerm.Error.Type);

        var withPerm = OrderStateMachine.ValidateStaffTransition(
            OrderStatusConstants.Placed,
            OrderStatusConstants.Confirmed,
            OrderPaymentStatusConstants.Unpaid,
            callerPermissions: ["orders.process"],
            cancelReason: null,
            codAmount: null,
            expectedTotal: 100_000);

        Assert.True(withPerm.IsSuccess);
    }

    [Fact]
    public void StaffTransition_ConfirmedToPreparing_RequiresOrdersProcessPermission()
    {
        var result = OrderStateMachine.ValidateStaffTransition(
            OrderStatusConstants.Confirmed,
            OrderStatusConstants.Preparing,
            OrderPaymentStatusConstants.Unpaid,
            callerPermissions: ["orders.process"],
            cancelReason: null,
            codAmount: null,
            expectedTotal: 100_000);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void StaffTransition_PreparingToPacked_RequiresOrdersProcessPermission()
    {
        var result = OrderStateMachine.ValidateStaffTransition(
            OrderStatusConstants.Preparing,
            OrderStatusConstants.Packed,
            OrderPaymentStatusConstants.Unpaid,
            callerPermissions: ["orders.process"],
            cancelReason: null,
            codAmount: null,
            expectedTotal: 100_000);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void StaffTransition_PackedToShipping_RequiresOrdersShipPermission()
    {
        var withProcessOnly = OrderStateMachine.ValidateStaffTransition(
            OrderStatusConstants.Packed,
            OrderStatusConstants.Shipping,
            OrderPaymentStatusConstants.Unpaid,
            callerPermissions: ["orders.process"],
            cancelReason: null,
            codAmount: null,
            expectedTotal: 100_000);

        Assert.False(withProcessOnly.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, withProcessOnly.Error.Type);

        var withShip = OrderStateMachine.ValidateStaffTransition(
            OrderStatusConstants.Packed,
            OrderStatusConstants.Shipping,
            OrderPaymentStatusConstants.Unpaid,
            callerPermissions: ["orders.ship"],
            cancelReason: null,
            codAmount: null,
            expectedTotal: 100_000);

        Assert.True(withShip.IsSuccess);
    }

    [Fact]
    public void StaffTransition_ShippingToDelivered_ValidatesCodAmountMatch()
    {
        // Amount mismatch should fail with CodAmountMismatch
        var mismatchResult = OrderStateMachine.ValidateStaffTransition(
            OrderStatusConstants.Shipping,
            OrderStatusConstants.Delivered,
            OrderPaymentStatusConstants.Unpaid,
            callerPermissions: ["orders.ship"],
            cancelReason: null,
            codAmount: 80_000,
            expectedTotal: 100_000);

        Assert.False(mismatchResult.IsSuccess);
        Assert.Equal(ErrorCodes.CodAmountMismatch, mismatchResult.Error.Code);

        // Matching amount succeeds
        var matchResult = OrderStateMachine.ValidateStaffTransition(
            OrderStatusConstants.Shipping,
            OrderStatusConstants.Delivered,
            OrderPaymentStatusConstants.Unpaid,
            callerPermissions: ["orders.ship"],
            cancelReason: null,
            codAmount: 100_000,
            expectedTotal: 100_000);

        Assert.True(matchResult.IsSuccess);
    }

    [Fact]
    public void StaffTransition_ShippingToDeliveryFailed_RequiresReason()
    {
        var missingReason = OrderStateMachine.ValidateStaffTransition(
            OrderStatusConstants.Shipping,
            OrderStatusConstants.DeliveryFailed,
            OrderPaymentStatusConstants.Unpaid,
            callerPermissions: ["orders.ship"],
            cancelReason: "",
            codAmount: null,
            expectedTotal: 100_000);

        Assert.False(missingReason.IsSuccess);
        Assert.Equal(ErrorType.Validation, missingReason.Error.Type);

        var withReason = OrderStateMachine.ValidateStaffTransition(
            OrderStatusConstants.Shipping,
            OrderStatusConstants.DeliveryFailed,
            OrderPaymentStatusConstants.Unpaid,
            callerPermissions: ["orders.ship"],
            cancelReason: "Customer unreachable after 3 calls",
            codAmount: null,
            expectedTotal: 100_000);

        Assert.True(withReason.IsSuccess);
    }

    [Fact]
    public void StaffTransition_DeliveryFailedToShipping_RetryDeliveryAllowed()
    {
        var retryResult = OrderStateMachine.ValidateStaffTransition(
            OrderStatusConstants.DeliveryFailed,
            OrderStatusConstants.Shipping,
            OrderPaymentStatusConstants.Unpaid,
            callerPermissions: ["orders.ship"],
            cancelReason: null,
            codAmount: null,
            expectedTotal: 100_000);

        Assert.True(retryResult.IsSuccess);
    }

    [Fact]
    public void StaffCancel_UnpaidEarlyOrder_CanBeCancelledByOrdersProcess()
    {
        var result = OrderStateMachine.ValidateStaffTransition(
            OrderStatusConstants.Placed,
            OrderStatusConstants.Cancelled,
            OrderPaymentStatusConstants.Unpaid,
            callerPermissions: ["orders.process"],
            cancelReason: "Out of stock on perishable items",
            codAmount: null,
            expectedTotal: 100_000);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void StaffCancel_PaidEarlyOrder_RequiresOrdersCancelPermission()
    {
        // orders.process only is forbidden for paid order per DEC-011 / Q-006
        var processOnly = OrderStateMachine.ValidateStaffTransition(
            OrderStatusConstants.Placed,
            OrderStatusConstants.Cancelled,
            OrderPaymentStatusConstants.Paid,
            callerPermissions: ["orders.process"],
            cancelReason: "Customer requested cancellation",
            codAmount: null,
            expectedTotal: 100_000);

        Assert.False(processOnly.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, processOnly.Error.Type);

        // orders.cancel succeeds
        var withCancelPerm = OrderStateMachine.ValidateStaffTransition(
            OrderStatusConstants.Placed,
            OrderStatusConstants.Cancelled,
            OrderPaymentStatusConstants.Paid,
            callerPermissions: ["orders.cancel"],
            cancelReason: "Customer requested cancellation",
            codAmount: null,
            expectedTotal: 100_000);

        Assert.True(withCancelPerm.IsSuccess);
    }

    [Fact]
    public void StaffCancel_PreparingOrPackedOrder_RequiresOrdersCancelPermission()
    {
        var processOnly = OrderStateMachine.ValidateStaffTransition(
            OrderStatusConstants.Preparing,
            OrderStatusConstants.Cancelled,
            OrderPaymentStatusConstants.Unpaid,
            callerPermissions: ["orders.process"],
            cancelReason: "Damaged during packing",
            codAmount: null,
            expectedTotal: 100_000);

        Assert.False(processOnly.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, processOnly.Error.Type);

        var withCancelPerm = OrderStateMachine.ValidateStaffTransition(
            OrderStatusConstants.Preparing,
            OrderStatusConstants.Cancelled,
            OrderPaymentStatusConstants.Unpaid,
            callerPermissions: ["orders.cancel"],
            cancelReason: "Damaged during packing",
            codAmount: null,
            expectedTotal: 100_000);

        Assert.True(withCancelPerm.IsSuccess);
    }

    [Theory]
    [InlineData(OrderStatusConstants.Placed, OrderStatusConstants.Shipping)]
    [InlineData(OrderStatusConstants.Placed, OrderStatusConstants.Delivered)]
    [InlineData(OrderStatusConstants.Preparing, OrderStatusConstants.Shipping)]
    [InlineData(OrderStatusConstants.Delivered, OrderStatusConstants.Cancelled)]
    [InlineData(OrderStatusConstants.Cancelled, OrderStatusConstants.Placed)]
    public void StaffTransition_InvalidJumps_FailsWithConflict(string current, string target)
    {
        var result = OrderStateMachine.ValidateStaffTransition(
            current,
            target,
            OrderPaymentStatusConstants.Unpaid,
            callerPermissions: ["orders.process", "orders.ship", "orders.cancel"],
            cancelReason: "Testing invalid jump",
            codAmount: 100_000,
            expectedTotal: 100_000);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.OrderInvalidTransition, result.Error.Code);
    }
}
