using GroceryHub.Modules.Customers.Application.DTOs;
using GroceryHub.Modules.Customers.Application.Services;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GroceryHub.UnitTests.Customers;

public sealed class AddressAndPhoneTests
{
    private sealed class NoOpAuditLogPort : IAuditLogPort
    {
        public Task<Result> WriteAsync(WriteAuditLogCommand command, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
    }

    [Theory]
    [InlineData("0901234567", true)]
    [InlineData("0912345678", true)]
    [InlineData("0987654321", true)]
    [InlineData("0387654321", true)]
    [InlineData("0701234567", true)]
    [InlineData("0561234567", true)]
    [InlineData("0812345678", true)]
    [InlineData("+84901234567", true)]
    [InlineData("84901234567", true)]
    [InlineData("090 123 4567", true)]
    [InlineData("090-123-4567", true)]
    [InlineData("0123456789", false)]  // 01 prefix invalid
    [InlineData("0223456789", false)]  // 02 prefix landline
    [InlineData("0423456789", false)]  // 04 prefix invalid
    [InlineData("0623456789", false)]  // 06 prefix invalid
    [InlineData("090123456", false)]    // 9 digits
    [InlineData("09012345678", false)]  // 11 digits starting with 09
    [InlineData("abcdefghij", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void VietnamesePhone_Validation_BR17(string phone, bool expectedValid)
    {
        bool isValid = VietnamesePhone.IsValid(phone);
        Assert.Equal(expectedValid, isValid);
    }

    [Fact]
    public async Task CreateAddress_WhenTenActiveAddressesExist_ReturnsAddressLimitReached()
    {
        var options = new DbContextOptionsBuilder<GroceryHubDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new GroceryHubDbContext(options);
        var timeProvider = TimeProvider.System;
        var service = new CustomersService(dbContext, timeProvider, new NoOpAuditLogPort());

        var userId = Guid.NewGuid();
        var now = timeProvider.GetUtcNow();

        // Seed province and ward
        dbContext.Provinces.Add(new Province
        {
            Code = "79",
            Name = "Thành phố Hồ Chí Minh",
            FullName = "Thành phố Hồ Chí Minh",
            Type = "Thành phố Trung ương"
        });
        dbContext.Wards.Add(new Ward
        {
            Code = "79001",
            Name = "Bến Nghé",
            FullName = "Phường Bến Nghé",
            Type = "Phường",
            ProvinceCode = "79"
        });

        // Seed 10 active addresses for userId
        for (int i = 0; i < 10; i++)
        {
            dbContext.Addresses.Add(new Address
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RecipientName = $"Người nhận {i}",
                Phone = "0901234567",
                ProvinceCode = "79",
                WardCode = "79001",
                Street = $"{i} Đường Lê Lợi",
                IsDefault = i == 0,
                CreatedAt = now
            });
        }

        // Add 1 soft-deleted address to prove deleted addresses don't count
        dbContext.Addresses.Add(new Address
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RecipientName = "Người nhận cũ",
            Phone = "0901234567",
            ProvinceCode = "79",
            WardCode = "79001",
            Street = "Đã xóa",
            IsDefault = false,
            CreatedAt = now.AddDays(-10),
            DeletedAt = now.AddDays(-1)
        });

        await dbContext.SaveChangesAsync();

        // 11th address attempt
        var request = new CreateAddressRequest(
            RecipientName: "Người nhận thứ 11",
            Phone: "0909999999",
            ProvinceCode: "79",
            WardCode: "79001",
            Street: "11 Lê Duẩn",
            Note: null,
            IsDefault: false);

        var result = await service.CreateAddressAsync(userId, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.AddressLimitReached, result.Error.Code);
    }
}
