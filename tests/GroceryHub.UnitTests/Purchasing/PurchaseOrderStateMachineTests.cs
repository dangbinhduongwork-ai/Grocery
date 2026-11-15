using GroceryHub.Modules.Purchasing.Domain;
using GroceryHub.SharedKernel;
using Xunit;

namespace GroceryHub.UnitTests.Purchasing;

public sealed class PurchaseOrderStateMachineTests
{
    [Theory]
    [InlineData(PurchaseOrderStates.Draft, PurchaseOrderStates.Submitted)]
    [InlineData(PurchaseOrderStates.Draft, PurchaseOrderStates.Ordered)]
    [InlineData(PurchaseOrderStates.Draft, PurchaseOrderStates.Cancelled)]
    [InlineData(PurchaseOrderStates.Submitted, PurchaseOrderStates.PartiallyReceived)]
    [InlineData(PurchaseOrderStates.Submitted, PurchaseOrderStates.Received)]
    [InlineData(PurchaseOrderStates.Submitted, PurchaseOrderStates.Cancelled)]
    [InlineData(PurchaseOrderStates.Ordered, PurchaseOrderStates.PartiallyReceived)]
    [InlineData(PurchaseOrderStates.Ordered, PurchaseOrderStates.Received)]
    [InlineData(PurchaseOrderStates.Ordered, PurchaseOrderStates.Cancelled)]
    [InlineData(PurchaseOrderStates.PartiallyReceived, PurchaseOrderStates.PartiallyReceived)]
    [InlineData(PurchaseOrderStates.PartiallyReceived, PurchaseOrderStates.Received)]
    [InlineData(PurchaseOrderStates.PartiallyReceived, PurchaseOrderStates.Closed)]
    [InlineData(PurchaseOrderStates.Received, PurchaseOrderStates.Closed)]
    public void CanTransition_ValidTransitions_ReturnsTrue(string from, string to)
    {
        bool allowed = PurchaseOrderStateMachine.CanTransition(from, to, totalReceivedQuantity: 0);
        Assert.True(allowed, $"Expected transition {from} -> {to} to be valid.");

        Result result = PurchaseOrderStateMachine.ValidateTransition(from, to, totalReceivedQuantity: 0);
        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(PurchaseOrderStates.Draft, PurchaseOrderStates.Closed)]
    [InlineData(PurchaseOrderStates.Draft, PurchaseOrderStates.PartiallyReceived)]
    [InlineData(PurchaseOrderStates.Draft, PurchaseOrderStates.Received)]
    [InlineData(PurchaseOrderStates.Submitted, PurchaseOrderStates.Draft)]
    [InlineData(PurchaseOrderStates.Submitted, PurchaseOrderStates.Closed)]
    [InlineData(PurchaseOrderStates.PartiallyReceived, PurchaseOrderStates.Draft)]
    [InlineData(PurchaseOrderStates.PartiallyReceived, PurchaseOrderStates.Cancelled)]
    [InlineData(PurchaseOrderStates.Received, PurchaseOrderStates.Draft)]
    [InlineData(PurchaseOrderStates.Received, PurchaseOrderStates.Cancelled)]
    [InlineData(PurchaseOrderStates.Received, PurchaseOrderStates.PartiallyReceived)]
    [InlineData(PurchaseOrderStates.Closed, PurchaseOrderStates.Draft)]
    [InlineData(PurchaseOrderStates.Closed, PurchaseOrderStates.Submitted)]
    [InlineData(PurchaseOrderStates.Closed, PurchaseOrderStates.Received)]
    [InlineData(PurchaseOrderStates.Cancelled, PurchaseOrderStates.Draft)]
    [InlineData(PurchaseOrderStates.Cancelled, PurchaseOrderStates.Submitted)]
    public void CanTransition_InvalidTransitions_ReturnsFalse(string from, string to)
    {
        bool allowed = PurchaseOrderStateMachine.CanTransition(from, to, totalReceivedQuantity: 0);
        Assert.False(allowed, $"Expected transition {from} -> {to} to be invalid.");

        Result result = PurchaseOrderStateMachine.ValidateTransition(from, to, totalReceivedQuantity: 0);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.PoInvalidState, result.Error.Code);
    }

    [Fact]
    public void CancelPO_WhenUnitsAlreadyReceived_IsBlocked()
    {
        // If 5 units have been received, cancellation from Submitted/Ordered must be rejected
        bool allowed = PurchaseOrderStateMachine.CanTransition(PurchaseOrderStates.Submitted, PurchaseOrderStates.Cancelled, totalReceivedQuantity: 5);
        Assert.False(allowed);

        Result result = PurchaseOrderStateMachine.ValidateTransition(PurchaseOrderStates.Submitted, PurchaseOrderStates.Cancelled, totalReceivedQuantity: 5);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.PoInvalidState, result.Error.Code);
    }

    [Fact]
    public void CancelPO_WhenNoUnitsReceived_IsAllowed()
    {
        bool allowed = PurchaseOrderStateMachine.CanTransition(PurchaseOrderStates.Submitted, PurchaseOrderStates.Cancelled, totalReceivedQuantity: 0);
        Assert.True(allowed);

        Result result = PurchaseOrderStateMachine.ValidateTransition(PurchaseOrderStates.Submitted, PurchaseOrderStates.Cancelled, totalReceivedQuantity: 0);
        Assert.True(result.IsSuccess);
    }
}
