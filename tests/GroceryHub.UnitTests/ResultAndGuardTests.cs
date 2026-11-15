using GroceryHub.SharedKernel;
using Xunit;

namespace GroceryHub.UnitTests;

public sealed class ResultAndGuardTests
{
    [Fact]
    public void Result_Success_HasNoErrorAndIsSuccessTrue()
    {
        Result result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(DomainError.None, result.Error);
    }

    [Fact]
    public void Result_Failure_CarriesDomainErrorAndIsFailureTrue()
    {
        DomainError error = DomainError.Conflict(ErrorCodes.InsufficientStock, "Out of stock.");

        Result result = Result.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Result_FailureWithDomainErrorNone_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure(DomainError.None));
    }

    [Fact]
    public void GenericResult_SuccessAndImplicitConversion_ExposeValue()
    {
        Result<int> explicitSuccess = Result.Success(42);
        Result<string> implicitSuccess = "fresh-milk";

        Assert.True(explicitSuccess.IsSuccess);
        Assert.Equal(42, explicitSuccess.Value);
        Assert.True(implicitSuccess.IsSuccess);
        Assert.Equal("fresh-milk", implicitSuccess.Value);
    }

    [Fact]
    public void GenericResult_Failure_ThrowsInvalidOperationExceptionWhenAccessingValue()
    {
        DomainError error = DomainError.NotFound("Product not found.");
        Result<Guid> failed = error;

        Assert.True(failed.IsFailure);
        Assert.Equal(error, failed.Error);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => _ = failed.Value);
        Assert.Contains(ErrorCodes.NotFound, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DomainError_Factories_PopulateExpectedCodesTypesAndFieldErrors()
    {
        var fieldErrors = new Dictionary<string, string[]>
        {
            ["quantity"] = ["Quantity must be between 1 and 20."]
        };

        DomainError validation = DomainError.Validation(ErrorCodes.QuantityLimitExceeded, "Invalid quantity.", fieldErrors);
        Assert.Equal(ErrorCodes.QuantityLimitExceeded, validation.Code);
        Assert.Equal(ErrorType.Validation, validation.Type);
        Assert.Same(fieldErrors, validation.FieldErrors);

        DomainError validationFailed = DomainError.ValidationFailed(fieldErrors);
        Assert.Equal(ErrorCodes.ValidationFailed, validationFailed.Code);
        Assert.Equal(ErrorType.Validation, validationFailed.Type);
        Assert.Same(fieldErrors, validationFailed.FieldErrors);

        DomainError badRequest = DomainError.BadRequest(ErrorCodes.BadRequest, "Malformed payload.");
        Assert.Equal(ErrorType.BadRequest, badRequest.Type);

        DomainError unauthorized = DomainError.Unauthorized();
        Assert.Equal(ErrorCodes.Unauthenticated, unauthorized.Code);
        Assert.Equal(ErrorType.Unauthorized, unauthorized.Type);

        DomainError forbidden = DomainError.Forbidden();
        Assert.Equal(ErrorCodes.Forbidden, forbidden.Code);
        Assert.Equal(ErrorType.Forbidden, forbidden.Type);

        DomainError notFound = DomainError.NotFound();
        Assert.Equal(ErrorCodes.NotFound, notFound.Code);
        Assert.Equal(ErrorType.NotFound, notFound.Type);

        DomainError conflict = DomainError.Conflict(ErrorCodes.CouponExpired, "Coupon expired.");
        Assert.Equal(ErrorCodes.CouponExpired, conflict.Code);
        Assert.Equal(ErrorType.Conflict, conflict.Type);

        DomainError concurrency = DomainError.Concurrency();
        Assert.Equal(ErrorCodes.ConcurrencyConflict, concurrency.Code);
        Assert.Equal(ErrorType.Concurrency, concurrency.Type);

        DomainError rateLimited = DomainError.RateLimited();
        Assert.Equal(ErrorCodes.RateLimited, rateLimited.Code);
        Assert.Equal(ErrorType.RateLimited, rateLimited.Type);

        DomainError unexpected = DomainError.Unexpected();
        Assert.Equal(ErrorCodes.InternalError, unexpected.Code);
        Assert.Equal(ErrorType.Unexpected, unexpected.Type);
    }

    [Fact]
    public void Guard_Methods_ValidateInputsAndThrowExpectedExceptions()
    {
        object validObject = new();
        Assert.Same(validObject, Guard.AgainstNull(validObject));
        Assert.Throws<ArgumentNullException>(() => Guard.AgainstNull<object>(null));

        Assert.Equal("Organic Kale", Guard.AgainstNullOrWhiteSpace("  Organic Kale  "));
        Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrWhiteSpace(null));
        Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrWhiteSpace("   "));

        Assert.Equal("SKU-01", Guard.AgainstExceedingLength("SKU-01", 10));
        Assert.Throws<ArgumentNullException>(() => Guard.AgainstExceedingLength(null!, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => Guard.AgainstExceedingLength("0123456789A", 10));

        Assert.Equal(5, Guard.AgainstOutOfRange(5, 1, 20));
        Assert.Throws<ArgumentOutOfRangeException>(() => Guard.AgainstOutOfRange(0, 1, 20));
        Assert.Throws<ArgumentOutOfRangeException>(() => Guard.AgainstOutOfRange(21, 1, 20));

        Assert.Equal(0L, Guard.AgainstNegative(0L));
        Assert.Equal(100L, Guard.AgainstNegative(100L));
        Assert.Throws<ArgumentOutOfRangeException>(() => Guard.AgainstNegative(-1L));

        Guid id = Guid.NewGuid();
        Assert.Equal(id, Guard.AgainstEmptyGuid(id));
        Assert.Throws<ArgumentException>(() => Guard.AgainstEmptyGuid(Guid.Empty));
    }

    [Theory]
    [InlineData(0, 24, 1, 24, 0)]
    [InlineData(-5, 0, 1, 24, 0)]
    [InlineData(1, -10, 1, 24, 0)]
    [InlineData(2, 10, 2, 10, 10)]
    [InlineData(3, 250, 3, 100, 200)]
    public void PageRequest_ClampsPageAndPageSizeWithinAllowedBounds(
        int inputPage,
        int inputPageSize,
        int expectedPage,
        int expectedPageSize,
        int expectedSkip)
    {
        PageRequest request = new(inputPage, inputPageSize);

        Assert.Equal(expectedPage, request.Page);
        Assert.Equal(expectedPageSize, request.PageSize);
        Assert.Equal(expectedSkip, request.Skip);
    }

    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(-4, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(25, 10, 3)]
    public void PagedList_ComputesTotalPagesCorrectly(int totalItems, int pageSize, int expectedTotalPages)
    {
        PagedList<string> paged = PagedList<string>.Create(["item-1"], page: 1, pageSize: pageSize, totalItems: totalItems);

        Assert.Equal(expectedTotalPages, paged.TotalPages);
        Assert.Equal(Math.Max(0, totalItems), paged.TotalItems);
    }

    [Fact]
    public void PagedList_Empty_ReturnsZeroItemsAndZeroTotalPages()
    {
        PagedList<int> empty = PagedList<int>.Empty(page: 2, pageSize: 20);

        Assert.Empty(empty.Items);
        Assert.Equal(2, empty.Page);
        Assert.Equal(20, empty.PageSize);
        Assert.Equal(0, empty.TotalItems);
        Assert.Equal(0, empty.TotalPages);
    }
}
