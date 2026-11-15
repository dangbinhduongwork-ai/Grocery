using GroceryHub.SharedKernel;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace GroceryHub.UnitTests;

public sealed class VietnameseConventionsTests
{
    [Theory]
    [InlineData("0901234567", "0901234567")]
    [InlineData("+84901234567", "0901234567")]
    [InlineData("84901234567", "0901234567")]
    [InlineData(" +84 90 123 4567 ", "0901234567")]
    [InlineData("0321234567", "0321234567")]
    [InlineData("0561234567", "0561234567")]
    [InlineData("0701234567", "0701234567")]
    [InlineData("0811234567", "0811234567")]
    public void VietnamesePhone_Br17_ValidPhoneNumbers_AreAcceptedAndNormalized(string rawPhone, string expectedNormalized)
    {
        Assert.True(VietnamesePhone.IsValid(rawPhone));
        Assert.Equal(expectedNormalized, VietnamesePhone.Normalize(rawPhone));

        Result<string> result = VietnamesePhone.TryNormalize(rawPhone);
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedNormalized, result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0123456789")]
    [InlineData("0281234567")]
    [InlineData("090123456")]
    [InlineData("09012345678")]
    [InlineData("+84123456789")]
    [InlineData("not-a-phone")]
    public void VietnamesePhone_Br17_InvalidPhoneNumbers_AreRejectedWithValidationError(string? invalidPhone)
    {
        Assert.False(VietnamesePhone.IsValid(invalidPhone));

        Result<string> result = VietnamesePhone.TryNormalize(invalidPhone);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.ValidationFailed, result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.NotNull(result.Error.FieldErrors);
        Assert.True(result.Error.FieldErrors!.ContainsKey("phone"));
    }

    [Fact]
    public void VietnameseTextNormalizer_StripsDiacriticsHandlesDStrokeAndGeneratesSlug()
    {
        const string input = "Rau củ quả tươi — Đường Ưu Đãi";

        string normalized = VietnameseTextNormalizer.RemoveDiacritics(input);
        string searchNormalized = VietnameseTextNormalizer.NormalizeForSearch("Rau củ quả tươi", "Đường Ưu Đãi");
        string slug = VietnameseTextNormalizer.ToSlug(input);

        Assert.Equal("rau cu qua tuoi duong uu dai", normalized);
        Assert.Equal("rau cu qua tuoi duong uu dai", searchNormalized);
        Assert.Equal("rau-cu-qua-tuoi-duong-uu-dai", slug);
        Assert.Equal(string.Empty, VietnameseTextNormalizer.RemoveDiacritics("   "));
        Assert.Equal(string.Empty, VietnameseTextNormalizer.NormalizeForSearch());
    }

    [Fact]
    public void VietnamTimeAndSystemClock_WithFakeTimeProvider_ComputeVietnamDayBoundariesAndOrderPrefix()
    {
        // 2026-11-14 20:30:00 UTC == 2026-11-15 03:30:00 +07:00 (Vietnam next calendar day)
        DateTimeOffset utcTimestamp = new(2026, 11, 14, 20, 30, 0, TimeSpan.Zero);
        FakeTimeProvider fakeTimeProvider = new(utcTimestamp);
        SystemClock clock = new(fakeTimeProvider);

        Assert.Equal(utcTimestamp, clock.UtcNow);

        DateTimeOffset vnNow = clock.VietnamNow;
        Assert.Equal(TimeSpan.FromHours(7), vnNow.Offset);
        Assert.Equal(new DateTime(2026, 11, 15, 3, 30, 0), vnNow.DateTime);
        Assert.Equal(vnNow, VietnamTime.ToVietnamTime(utcTimestamp));

        DateOnly vnDate = VietnamTime.ToVietnamDate(utcTimestamp);
        Assert.Equal(new DateOnly(2026, 11, 15), vnDate);

        DateTimeOffset startOfVnDay = VietnamTime.StartOfVietnamDay(utcTimestamp);
        Assert.Equal(new DateTimeOffset(2026, 11, 15, 0, 0, 0, TimeSpan.FromHours(7)), startOfVnDay);

        DateTimeOffset startOfVnDayUtc = VietnamTime.StartOfVietnamDayUtc(vnDate);
        Assert.Equal(new DateTimeOffset(2026, 11, 14, 17, 0, 0, TimeSpan.Zero), startOfVnDayUtc);

        DateTimeOffset endOfVnDayUtc = VietnamTime.EndOfVietnamDayExclusiveUtc(vnDate);
        Assert.Equal(new DateTimeOffset(2026, 11, 15, 17, 0, 0, TimeSpan.Zero), endOfVnDayUtc);

        string orderPrefix = VietnamTime.FormatOrderDatePrefix(utcTimestamp);
        Assert.Equal("261115", orderPrefix);

        Guid v7Guid = clock.NewSequentialGuid();
        Assert.NotEqual(Guid.Empty, v7Guid);
        Assert.Equal(7, v7Guid.Version);
    }
}
