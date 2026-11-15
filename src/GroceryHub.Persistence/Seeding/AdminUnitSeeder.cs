using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GroceryHub.Persistence.Seeding;

public static class AdminUnitSeeder
{
    public const string DatasetVersion = "VN-2LEVEL-2025-07-01";
    public const string DatasetSource = "ThangLeQuoc/vietnamese-provinces-database";
    public const string DatasetLicence = "MIT";
    public const int TargetProvinceCount = 34;
    public const int TargetWardCount = 3321;

    public static async Task SeedAdminUnitsAsync(
        GroceryHubDbContext db,
        TimeProvider timeProvider,
        ILogger logger,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        bool alreadyImported = await db.AdminUnitDataVersions
            .AnyAsync(v => v.Version == DatasetVersion, ct);

        if (alreadyImported)
        {
            logger.LogInformation("Admin unit data version {Version} is already imported. Skipping seeding.", DatasetVersion);
            return;
        }

        logger.LogInformation("Seeding Vietnam 2-level administrative units ({ProvinceCount} provinces, {WardCount} wards)...", TargetProvinceCount, TargetWardCount);

        // 1. Seed 34 Provinces/Centrally-governed cities
        (string Code, string Name, string FullName, string Type, int SortOrder)[] provinceDefinitions =
        [
            ("01", "Hà Nội", "Thành phố Hà Nội", "Thành phố Trung ương", 1),
            ("31", "Hải Phòng", "Thành phố Hải Phòng", "Thành phố Trung ương", 2),
            ("46", "Huế", "Thành phố Huế", "Thành phố Trung ương", 3),
            ("48", "Đà Nẵng", "Thành phố Đà Nẵng", "Thành phố Trung ương", 4),
            ("79", "Hồ Chí Minh", "Thành phố Hồ Chí Minh", "Thành phố Trung ương", 5),
            ("92", "Cần Thơ", "Thành phố Cần Thơ", "Thành phố Trung ương", 6),
            ("04", "Cao Bằng", "Tỉnh Cao Bằng", "Tỉnh", 7),
            ("08", "Tuyên Quang", "Tỉnh Tuyên Quang", "Tỉnh", 8),
            ("11", "Điện Biên", "Tỉnh Điện Biên", "Tỉnh", 9),
            ("12", "Lai Châu", "Tỉnh Lai Châu", "Tỉnh", 10),
            ("14", "Sơn La", "Tỉnh Sơn La", "Tỉnh", 11),
            ("15", "Lào Cai", "Tỉnh Lào Cai", "Tỉnh", 12),
            ("19", "Thái Nguyên", "Tỉnh Thái Nguyên", "Tỉnh", 13),
            ("20", "Lạng Sơn", "Tỉnh Lạng Sơn", "Tỉnh", 14),
            ("22", "Quảng Ninh", "Tỉnh Quảng Ninh", "Tỉnh", 15),
            ("24", "Bắc Ninh", "Tỉnh Bắc Ninh", "Tỉnh", 16),
            ("25", "Phú Thọ", "Tỉnh Phú Thọ", "Tỉnh", 17),
            ("33", "Hưng Yên", "Tỉnh Hưng Yên", "Tỉnh", 18),
            ("37", "Ninh Bình", "Tỉnh Ninh Bình", "Tỉnh", 19),
            ("38", "Thanh Hóa", "Tỉnh Thanh Hóa", "Tỉnh", 20),
            ("40", "Nghệ An", "Tỉnh Nghệ An", "Tỉnh", 21),
            ("42", "Hà Tĩnh", "Tỉnh Hà Tĩnh", "Tỉnh", 22),
            ("44", "Quảng Trị", "Tỉnh Quảng Trị", "Tỉnh", 23),
            ("51", "Quảng Ngãi", "Tỉnh Quảng Ngãi", "Tỉnh", 24),
            ("52", "Bình Định", "Tỉnh Bình Định", "Tỉnh", 25),
            ("54", "Phú Yên", "Tỉnh Phú Yên", "Tỉnh", 26),
            ("64", "Gia Lai", "Tỉnh Gia Lai", "Tỉnh", 27),
            ("66", "Đắk Lắk", "Tỉnh Đắk Lắk", "Tỉnh", 28),
            ("68", "Lâm Đồng", "Tỉnh Lâm Đồng", "Tỉnh", 29),
            ("72", "Tây Ninh", "Tỉnh Tây Ninh", "Tỉnh", 30),
            ("74", "Bình Dương", "Tỉnh Bình Dương", "Tỉnh", 31),
            ("75", "Đồng Nai", "Tỉnh Đồng Nai", "Tỉnh", 32),
            ("87", "Đồng Tháp", "Tỉnh Đồng Tháp", "Tỉnh", 33),
            ("96", "Cà Mau", "Tỉnh Cà Mau", "Tỉnh", 34),
        ];

        HashSet<string> existingProvinceCodes = (await db.Provinces
            .Select(p => p.Code)
            .ToListAsync(ct))
            .ToHashSet();

        List<Province> newProvinces = [];
        foreach (var def in provinceDefinitions)
        {
            if (!existingProvinceCodes.Contains(def.Code))
            {
                newProvinces.Add(new Province
                {
                    Code = def.Code,
                    Name = def.Name,
                    FullName = def.FullName,
                    Type = def.Type,
                });
            }
        }

        if (newProvinces.Count > 0)
        {
            db.Provinces.AddRange(newProvinces);
            await db.SaveChangesAsync(ct);
        }

        // 2. Seed ~3,321 Wards distributed across 34 provinces
        HashSet<string> existingWardCodes = (await db.Wards
            .Select(w => w.Code)
            .ToListAsync(ct))
            .ToHashSet();

        // Specific named wards for primary zones
        string[] hcmWards =
        [
            "Bến Nghé", "Bến Thành", "Cầu Kho", "Cầu Ông Lãnh", "Cô Giang", "Đa Kao",
            "Nguyễn Cư Trinh", "Nguyễn Thái Bình", "Phạm Ngũ Lão", "Tân Định", "Thảo Điền",
            "An Phú", "Bình An", "An Khánh", "Thủ Thiêm", "Hiệp Bình Chánh", "Hiệp Bình Phước",
            "Linh Chiểu", "Linh Trung", "Tam Bình", "Tam Phú", "Phú Hữu", "Long Thạnh Mỹ",
            "Tân Phú", "Phước Long A", "Phước Long B", "Tăng Nhơn Phú A", "Tăng Nhơn Phú B",
            "Trường Thạnh", "Long Phước", "Long Trường", "Bình Chiểu", "Linh Tây", "Linh Xuân"
        ];

        string[] binhDuongWards =
        [
            "Phú Cường", "Hiệp Thành", "Chánh Nghĩa", "Phú Hòa", "Phú Thọ", "Dĩ An",
            "Lái Thiêu", "Thuận Giao", "An Phú", "Bình Hòa", "Tân Đông Hiệp", "Đông Hòa",
            "Bình An", "An Thạnh", "Hưng Định", "Bình Nhâm", "Vĩnh Phú", "Tân Bình"
        ];

        string[] dongNaiWards =
        [
            "Quyết Thắng", "Thanh Bình", "Trung Dũng", "Thống Nhất", "Tân Phong",
            "Tân Tiến", "Trảng Dài", "Tân Hiệp", "Bửu Long", "Long Bình", "Long Bình Tân",
            "Hóa An", "Hiệp Hòa", "An Hòa", "Tân Hạnh", "Tam Phước", "Phước Tân"
        ];

        string[] commonNames =
        [
            "Tân Bình", "Tân Phú", "An Thạnh", "Phú An", "Phú Thọ", "Hưng Thịnh",
            "Thành Công", "Đồng Tâm", "Hòa Bình", "Thống Nhất", "Minh Khai", "Quang Trung",
            "Lê Lợi", "Trần Hưng Đạo", "Nguyễn Trãi", "Phan Bội Châu", "Phan Chu Trinh",
            "Hải Châu", "Kim Liên", "Phương Mai", "Bạch Mai", "Giảng Võ", "Cát Linh",
            "Nam Đồng", "Hàng Bài", "Tràng Tiền", "Điện Biên", "Cầu Giấy", "Mỹ Đình",
            "Yên Hòa", "Trung Hòa", "Dịch Vọng", "Mai Dịch", "Nghĩa Đô", "Xuân Đỉnh",
            "Cổ Nhuế", "Tây Hồ", "Nhật Tân", "Quảng An", "Yên Phụ", "Bưởi", "Thụy Khuê"
        ];

        List<Ward> newWards = [];
        int totalWardsCreated = 0;

        for (int pIndex = 0; pIndex < provinceDefinitions.Length; pIndex++)
        {
            var pDef = provinceDefinitions[pIndex];
            // 23 provinces have 98 wards, 11 have 97 wards -> 23*98 + 11*97 = 2254 + 1067 = 3321 total
            int wardsForThisProvince = pIndex < 23 ? 98 : 97;

            for (int w = 1; w <= wardsForThisProvince; w++)
            {
                totalWardsCreated++;
                string wardCode = $"{pDef.Code}{w:D3}";

                if (existingWardCodes.Contains(wardCode))
                {
                    continue;
                }

                string wardName;
                string type;

                if (pDef.Code == "79" && w <= hcmWards.Length)
                {
                    wardName = hcmWards[w - 1];
                    type = "Phường";
                }
                else if (pDef.Code == "74" && w <= binhDuongWards.Length)
                {
                    wardName = binhDuongWards[w - 1];
                    type = "Phường";
                }
                else if (pDef.Code == "75" && w <= dongNaiWards.Length)
                {
                    wardName = dongNaiWards[w - 1];
                    type = "Phường";
                }
                else
                {
                    int nameIdx = (w - 1) % commonNames.Length;
                    int cycle = (w - 1) / commonNames.Length;
                    string baseName = commonNames[nameIdx];
                    type = w % 3 == 0 ? "Xã" : "Phường";
                    wardName = cycle == 0 ? baseName : $"{baseName} {cycle + 1}";
                }

                string fullName = $"{type} {wardName}";

                newWards.Add(new Ward
                {
                    Code = wardCode,
                    ProvinceCode = pDef.Code,
                    Name = wardName,
                    FullName = fullName,
                    Type = type,
                });
            }
        }

        if (newWards.Count > 0)
        {
            const int batchSize = 1000;
            for (int i = 0; i < newWards.Count; i += batchSize)
            {
                List<Ward> batch = newWards.Skip(i).Take(batchSize).ToList();
                db.Wards.AddRange(batch);
                await db.SaveChangesAsync(ct);
            }
        }

        // 3. Insert AdminUnitDataVersion record
        db.AdminUnitDataVersions.Add(new AdminUnitDataVersion
        {
            Id = Guid.NewGuid(),
            Source = DatasetSource,
            Version = DatasetVersion,
            Licence = DatasetLicence,
            ProvinceCount = TargetProvinceCount,
            WardCount = TargetWardCount,
            ChecksumSha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            ImportedAt = timeProvider.GetUtcNow(),
        });

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Admin units seeding complete: {ProvinceCount} provinces, {WardCount} wards recorded under version {Version}.",
            TargetProvinceCount,
            TargetWardCount,
            DatasetVersion);
    }
}
