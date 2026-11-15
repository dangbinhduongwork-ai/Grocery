using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GroceryHub.Persistence.Seeding;

public static class ShippingSeeder
{
    public const string Zone1Name = "TP. Hồ Chí Minh";
    public const string Zone2Name = "Bình Dương, Đồng Nai";

    public static async Task SeedShippingAsync(
        GroceryHubDbContext db,
        TimeProvider timeProvider,
        ILogger logger,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        DateTimeOffset now = timeProvider.GetUtcNow();

        // 1. Zone 1: TP. Hồ Chí Minh
        ShippingZone? zone1 = await db.ShippingZones
            .Include(z => z.ServiceableProvinces)
            .Include(z => z.ShippingRates)
            .FirstOrDefaultAsync(z => z.Name == Zone1Name, ct);

        if (zone1 is null)
        {
            zone1 = new ShippingZone
            {
                Id = Guid.NewGuid(),
                Name = Zone1Name,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            zone1.ServiceableProvinces.Add(new ShippingZoneProvince
            {
                ZoneId = zone1.Id,
                ProvinceCode = "79",
                CreatedAt = now
            });

            zone1.ShippingRates.Add(new ShippingRate
            {
                Id = Guid.NewGuid(),
                ZoneId = zone1.Id,
                BaseFee = 25_000L,
                FreeShippingThreshold = 300_000L,
                EstimatedDaysMin = 1,
                EstimatedDaysMax = 2,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });

            db.ShippingZones.Add(zone1);
            logger.LogInformation("Seeding shipping zone 1: {ZoneName}", Zone1Name);
        }
        else
        {
            if (!zone1.ServiceableProvinces.Any(p => p.ProvinceCode == "79"))
            {
                zone1.ServiceableProvinces.Add(new ShippingZoneProvince
                {
                    ZoneId = zone1.Id,
                    ProvinceCode = "79",
                    CreatedAt = now
                });
            }

            if (!zone1.ShippingRates.Any())
            {
                zone1.ShippingRates.Add(new ShippingRate
                {
                    Id = Guid.NewGuid(),
                    ZoneId = zone1.Id,
                    BaseFee = 25_000L,
                    FreeShippingThreshold = 300_000L,
                    EstimatedDaysMin = 1,
                    EstimatedDaysMax = 2,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        // 2. Zone 2: Bình Dương, Đồng Nai
        ShippingZone? zone2 = await db.ShippingZones
            .Include(z => z.ServiceableProvinces)
            .Include(z => z.ShippingRates)
            .FirstOrDefaultAsync(z => z.Name == Zone2Name, ct);

        if (zone2 is null)
        {
            zone2 = new ShippingZone
            {
                Id = Guid.NewGuid(),
                Name = Zone2Name,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            zone2.ServiceableProvinces.Add(new ShippingZoneProvince
            {
                ZoneId = zone2.Id,
                ProvinceCode = "74",
                CreatedAt = now
            });

            zone2.ServiceableProvinces.Add(new ShippingZoneProvince
            {
                ZoneId = zone2.Id,
                ProvinceCode = "75",
                CreatedAt = now
            });

            zone2.ShippingRates.Add(new ShippingRate
            {
                Id = Guid.NewGuid(),
                ZoneId = zone2.Id,
                BaseFee = 35_000L,
                FreeShippingThreshold = 300_000L,
                EstimatedDaysMin = 2,
                EstimatedDaysMax = 3,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });

            db.ShippingZones.Add(zone2);
            logger.LogInformation("Seeding shipping zone 2: {ZoneName}", Zone2Name);
        }
        else
        {
            if (!zone2.ServiceableProvinces.Any(p => p.ProvinceCode == "74"))
            {
                zone2.ServiceableProvinces.Add(new ShippingZoneProvince
                {
                    ZoneId = zone2.Id,
                    ProvinceCode = "74",
                    CreatedAt = now
                });
            }

            if (!zone2.ServiceableProvinces.Any(p => p.ProvinceCode == "75"))
            {
                zone2.ServiceableProvinces.Add(new ShippingZoneProvince
                {
                    ZoneId = zone2.Id,
                    ProvinceCode = "75",
                    CreatedAt = now
                });
            }

            if (!zone2.ShippingRates.Any())
            {
                zone2.ShippingRates.Add(new ShippingRate
                {
                    Id = Guid.NewGuid(),
                    ZoneId = zone2.Id,
                    BaseFee = 35_000L,
                    FreeShippingThreshold = 300_000L,
                    EstimatedDaysMin = 2,
                    EstimatedDaysMax = 3,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Shipping zones seeding completed successfully.");
    }
}
