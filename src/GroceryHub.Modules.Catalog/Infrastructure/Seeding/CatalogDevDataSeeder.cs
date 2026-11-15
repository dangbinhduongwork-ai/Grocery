using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GroceryHub.Modules.Catalog.Infrastructure.Seeding;

public static class CatalogDevDataSeeder
{
    public static async Task SeedSampleProductsAsync(
        GroceryHubDbContext db,
        TimeProvider timeProvider,
        ILogger logger,
        CancellationToken ct = default)
    {
        if (await db.Products.AnyAsync(ct))
        {
            logger.LogInformation("Products already seeded. Skipping CatalogDevDataSeeder.");
            return;
        }

        DateTimeOffset now = timeProvider.GetUtcNow();

        // 1. Seed Categories (8 Aisles per DEC-008)
        var categoryDefs = new (string Name, string Slug, string Description, int SortOrder)[]
        {
            ("Rau củ quả", "rau-cu-qua", "Rau xanh tươi sống, củ quả sạch VietGAP mỗi ngày", 1),
            ("Thịt cá trứng", "thit-ca-trung", "Thịt tươi sống, hải sản tươi ngon, trứng sạch an toàn", 2),
            ("Sữa & chế phẩm", "sua-che-pham", "Sữa tươi, sữa chua, bơ, phô mai và các loại sữa hạt", 3),
            ("Đồ khô & gia vị", "do-kho-gia-vi", "Gạo ngon, mì miến phở khô, gia vị, dầu ăn, nước chấm", 4),
            ("Đồ uống", "do-uong", "Nước khoáng, nước giải khát, trà, cà phê, nước trái cây", 5),
            ("Bánh kẹo & ăn vặt", "banh-keo-an-vat", "Bánh quy, snack giòn rụm, kẹo dẻo, hạt dinh dưỡng", 6),
            ("Chăm sóc cá nhân", "cham-soc-ca-nhan", "Dầu gội, sữa tắm, kem đánh răng, chăm sóc cơ thể", 7),
            ("Vệ sinh nhà cửa", "ve-sinh-nha-cua", "Nước giặt, nước xả, nước rửa chén, chất tẩy rửa gia dụng", 8)
        };

        Dictionary<string, Category> categories = new(StringComparer.OrdinalIgnoreCase);
        foreach (var def in categoryDefs)
        {
            var existing = await db.Categories.FirstOrDefaultAsync(c => c.Slug == def.Slug, ct);
            if (existing == null)
            {
                existing = new Category
                {
                    Id = Guid.NewGuid(),
                    Name = def.Name,
                    Slug = def.Slug,
                    Description = def.Description,
                    SortOrder = def.SortOrder,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.Categories.Add(existing);
            }
            categories[def.Slug] = existing;
        }

        // 2. Seed Brands
        var brandDefs = new (string Name, string Slug)[]
        {
            ("Vinamilk", "vinamilk"),
            ("TH True Milk", "th-true-milk"),
            ("C.P", "cp"),
            ("Ba Huân", "ba-huan"),
            ("Chin-su", "chin-su"),
            ("Nam Ngư", "nam-ngu"),
            ("Maggi", "maggi"),
            ("Knorr", "knorr"),
            ("Biên Hòa", "bien-hoa"),
            ("Simply", "simply"),
            ("Tường An", "tuong-an"),
            ("Hảo Hảo", "hao-hao"),
            ("Omachi", "omachi"),
            ("La Vie", "la-vie"),
            ("Coca-Cola", "coca-cola"),
            ("Pepsi", "pepsi"),
            ("Orion", "orion"),
            ("Kinh Đô", "kinh-do"),
            ("Sunsilk", "sunsilk"),
            ("Lifebuoy", "lifebuoy"),
            ("Omo", "omo"),
            ("Sunlight", "sunlight"),
            ("Vim", "vim"),
            ("Cif", "cif"),
            ("Đà Lạt GAP", "da-lat-gap")
        };

        Dictionary<string, Brand> brands = new(StringComparer.OrdinalIgnoreCase);
        foreach (var bDef in brandDefs)
        {
            var existing = await db.Brands.FirstOrDefaultAsync(b => b.Slug == bDef.Slug, ct);
            if (existing == null)
            {
                existing = new Brand
                {
                    Id = Guid.NewGuid(),
                    Name = bDef.Name,
                    Slug = bDef.Slug,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.Brands.Add(existing);
            }
            brands[bDef.Slug] = existing;
        }

        await db.SaveChangesAsync(ct);

        // 3. 200 Products definitions (25 per aisle across 8 aisles)
        var productTemplates = GetSampleProductDefinitions();
        logger.LogInformation("Seeding {Count} realistic sample products across 8 aisles...", productTemplates.Count);

        List<Product> productsToAdd = new(productTemplates.Count);
        List<StockItem> stockItemsToAdd = new(productTemplates.Count);

        int index = 1;
        foreach (var item in productTemplates)
        {
            Category category = categories[item.CategorySlug];
            Brand? brand = item.BrandSlug != null && brands.TryGetValue(item.BrandSlug, out var b) ? b : null;

            string sku = item.Sku;
            string slug = VietnameseTextNormalizer.ToSlug(item.Name);
            string searchText = VietnameseTextNormalizer.NormalizeForSearch(
                item.Name,
                sku,
                item.Barcode,
                category.Name,
                brand?.Name);

            Guid prodId = Guid.NewGuid();
            Product p = new()
            {
                Id = prodId,
                Sku = sku,
                Barcode = item.Barcode,
                Name = item.Name,
                Slug = $"{slug}-{index:D3}",
                SearchText = searchText,
                CategoryId = category.Id,
                BrandId = brand?.Id,
                Unit = item.Unit,
                PackSize = item.PackSize,
                ListPrice = item.ListPrice,
                SellingPrice = item.SellingPrice,
                CostPrice = (long)Math.Round(item.SellingPrice * 0.72), // BR-01
                Status = ProductStatus.Active,
                ShortDescription = item.ShortDescription,
                Description = $"{item.Name}. Nguồn gốc xuất xứ rõ ràng, tiêu chuẩn vệ sinh an toàn thực phẩm. Bảo quản nơi khô ráo, thoáng mát hoặc theo hướng dẫn trên bao bì.",
                IsFeatured = index % 8 == 0,
                RatingAvg = 4.70m + (decimal)(index % 30) / 100m,
                RatingCount = 10 + (index * 3) % 80,
                SoldCount = 50 + (index * 7) % 300,
                ExpiryTracked = item.CategorySlug == "rau-cu-qua" || item.CategorySlug == "thit-ca-trung" || item.CategorySlug == "sua-che-pham",
                CreatedAt = now.AddDays(-30).AddMinutes(index * 15),
                UpdatedAt = now
            };

            // Primary image
            p.Images.Add(new ProductImage
            {
                Id = Guid.NewGuid(),
                ProductId = prodId,
                StorageKey = $"sample_{item.CategorySlug}_{index:D3}.jpg",
                AltText = item.Name,
                SortOrder = 0,
                IsPrimary = true,
                Width = 800,
                Height = 800,
                CreatedAt = now
            });

            productsToAdd.Add(p);

            // Initial stock
            stockItemsToAdd.Add(new StockItem
            {
                ProductId = prodId,
                OnHand = 40 + (index * 5) % 60,
                Reserved = 0,
                ReorderLevel = 10,
                CreatedAt = now,
                UpdatedAt = now
            });

            index++;
        }

        db.Products.AddRange(productsToAdd);
        db.StockItems.AddRange(stockItemsToAdd);

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Successfully seeded {Count} products and stock items into catalog database.", productsToAdd.Count);
    }

    private static List<SampleProductItem> GetSampleProductDefinitions()
    {
        List<SampleProductItem> items = [];

        // 1. Rau củ quả (25 items)
        string cat1 = "rau-cu-qua";
        string bDalat = "da-lat-gap";
        items.AddRange([
            new(cat1, bDalat, "GH-RCQ-0001", "893500010001", "Cà chua bi Đà Lạt VietGAP", "Hộp", "500g", 25000, 22000, "Cà chua bi giòn ngọt, giàu vitamin C và chất chống oxy hóa."),
            new(cat1, bDalat, "GH-RCQ-0002", "893500010002", "Rau muống nước sạch thủy canh", "Túi", "500g", 18000, 15000, "Rau muống cọng non xanh, xào tỏi hoặc nấu canh đều thanh mát."),
            new(cat1, bDalat, "GH-RCQ-0003", "893500010003", "Xà lách mỡ Đà Lạt tươi sạch", "Cây", "300g", 22000, 19000, "Lá xà lách mềm giòn, thích hợp làm món salad hoặc ăn kèm."),
            new(cat1, bDalat, "GH-RCQ-0004", "893500010004", "Cà rốt Đà Lạt loại 1", "Túi", "500g", 24000, 20000, "Cà rốt tươi ngon, củ thẳng đẹp, giàu beta-carotene."),
            new(cat1, bDalat, "GH-RCQ-0005", "893500010005", "Khoai tây hồng Đà Lạt", "Túi", "1kg", 38000, 32000, "Khoai tây bùi thơm, ruột vàng, thích hợp chiên xào hầm canh."),
            new(cat1, bDalat, "GH-RCQ-0006", "893500010006", "Bắp cải thảo tươi Đà Lạt", "Bắp", "800g", 26000, 22000, "Bắp cải thảo cuộn chặt, giòn ngọt dùng nấu lẩu hoặc làm kim chi."),
            new(cat1, bDalat, "GH-RCQ-0007", "893500010007", "Dưa leo baby giòn ngọt", "Túi", "500g", 22000, 18000, "Dưa leo quả nhỏ ruột đặc, giòn ngọt thanh nhiệt."),
            new(cat1, bDalat, "GH-RCQ-0008", "893500010008", "Bí đỏ hồ lô giống Nhật", "Trái", "1kg", 25000, 20000, "Bí đỏ dẻo bùi, thơm ngọt, thích hợp nấu chè hoặc hầm canh."),
            new(cat1, bDalat, "GH-RCQ-0009", "893500010009", "Hành lá tươi làm sạch", "Bó", "200g", 12000, 10000, "Hành lá xanh mướt, thơm nồng gia vị bữa cơm gia đình."),
            new(cat1, null, "GH-RCQ-0010", "893500010010", "Tỏi cô đơn Lý Sơn đặc sản", "Hũ", "250g", 85000, 75000, "Tỏi một nhánh thơm nồng đặc trưng, vị cay ngọt tự nhiên."),
            new(cat1, null, "GH-RCQ-0011", "893500010011", "Gừng tươi Tây Nguyên", "Túi", "300g", 20000, 16000, "Gừng sẻ nhánh nhỏ, củ già thơm cay đậm vị."),
            new(cat1, null, "GH-RCQ-0012", "893500010012", "Chanh không hạt mọng nước", "Túi", "500g", 25000, 20000, "Chanh vỏ mỏng nhiều nước, thơm mát tự nhiên."),
            new(cat1, null, "GH-RCQ-0013", "893500010013", "Ớt hiểm đỏ chỉ thiên cay nồng", "Hộp", "100g", 10000, 8000, "Ớt hiểm tươi cay xé lưỡi, màu đỏ tươi rói."),
            new(cat1, bDalat, "GH-RCQ-0014", "893500010014", "Nấm đùi gà tươi loại 1", "Khay", "300g", 32000, 28000, "Nấm đùi gà thân trắng giòn sần sật, giàu đạm thực vật."),
            new(cat1, bDalat, "GH-RCQ-0015", "893500010015", "Nấm kim châm trắng tươi", "Gói", "150g", 16000, 13000, "Nấm kim châm dai giòn thanh ngọt, thích hợp nhúng lẩu."),
            new(cat1, bDalat, "GH-RCQ-0016", "893500010016", "Bông cải xanh Baby Đà Lạt", "Cây", "400g", 35000, 29000, "Bông cải xanh mướt, nhiều vitamin khoáng chất."),
            new(cat1, null, "GH-RCQ-0017", "893500010017", "Khoai lang mật Đà Lạt ngọt lịm", "Túi", "1kg", 35000, 30000, "Khoai lang mật nướng hoặc luộc dẻo quánh thơm ngon."),
            new(cat1, bDalat, "GH-RCQ-0018", "893500010018", "Củ dền đỏ Đà Lạt", "Túi", "500g", 24000, 19000, "Củ dền tươi giàu sắt, bổ máu, nấu canh súp bổ dưỡng."),
            new(cat1, null, "GH-RCQ-0019", "893500010019", "Mướp hương vườn nhà tươi non", "Trái", "600g", 20000, 16000, "Mướp hương thơm ngát tự nhiên, nấu canh tôm cực ngon."),
            new(cat1, null, "GH-RCQ-0020", "893500010020", "Khổ qua rừng tươi sạch", "Túi", "500g", 30000, 25000, "Khổ qua giòn rụm vị đắng thanh, giải nhiệt mát gan."),
            new(cat1, null, "GH-RCQ-0021", "893500010021", "Bầu sao tươi non ngọt nước", "Trái", "800g", 22000, 18000, "Bầu sao vỏ mỏng ruột mềm, nấu canh nghêu ngọt nước."),
            new(cat1, bDalat, "GH-RCQ-0022", "893500010022", "Đậu cove non Đà Lạt", "Túi", "400g", 25000, 21000, "Đậu cove quả thẳng xanh bóng, giòn ngọt xào thịt bò."),
            new(cat1, null, "GH-RCQ-0023", "893500010023", "Bắp ngọt Mỹ luộc giòn", "Trái", "350g", 14000, 11000, "Bắp ngọt hạt vàng đều tăm tắp, thơm béo."),
            new(cat1, null, "GH-RCQ-0024", "893500010024", "Chuối cau chín tự nhiên", "Nải", "1kg", 35000, 28000, "Chuối cau quả nhỏ đều, ngọt đậm đà bổ dưỡng."),
            new(cat1, null, "GH-RCQ-0025", "893500010025", "Xoài cát Chu thơm ngọt Đồng Tháp", "Kg", "1kg", 55000, 48000, "Xoài cát Chu ruột vàng tươi, thơm lừng mọng nước.")
        ]);

        // 2. Thịt cá trứng (25 items)
        string cat2 = "thit-ca-trung";
        string bCp = "cp";
        string bBaHuan = "ba-huan";
        items.AddRange([
            new(cat2, bCp, "GH-TCT-0001", "893500020001", "Thịt ba rọi heo C.P tươi sạch", "Khay", "500g", 85000, 75000, "Thịt ba rọi tỷ lệ mỡ nạc đan xen hoàn hảo, thơm mềm."),
            new(cat2, bCp, "GH-TCT-0002", "893500020002", "Thịt đùi heo C.P làm sạch", "Khay", "500g", 68000, 59000, "Thịt đùi mềm nạc, kho tiêu hoặc luộc chấm mắm tôm ngon miệng."),
            new(cat2, bCp, "GH-TCT-0003", "893500020003", "Thịt nạc dăm heo C.P", "Khay", "500g", 78000, 68000, "Thịt nạc dăm nhiều vân mỡ mịn, nướng hoặc xào không bị khô."),
            new(cat2, bCp, "GH-TCT-0004", "893500020004", "Sườn non heo C.P thượng hạng", "Khay", "500g", 115000, 102000, "Sườn non tươi nhiều nạc, cọng sườn nhỏ nấu canh chua hay kho rim."),
            new(cat2, bCp, "GH-TCT-0005", "893500020005", "Thịt gà ta làm sạch C.P", "Con", "1.2kg", 145000, 129000, "Gà ta thả vườn da vàng thịt dai ngọt, luộc lá chanh cực thơm."),
            new(cat2, bCp, "GH-TCT-0006", "893500020006", "Cánh gà tươi C.P làm sạch", "Khay", "500g", 62000, 54000, "Cánh gà tươi giòn sụn, chiên nước mắm thơm lừng."),
            new(cat2, bCp, "GH-TCT-0007", "893500020007", "Đùi tỏi gà C.P loại lớn", "Khay", "500g", 55000, 48000, "Đùi gà chắc thịt, rán giòn hoặc nướng sốt cay hấp dẫn."),
            new(cat2, bBaHuan, "GH-TCT-0008", "893500020008", "Trứng gà tươi Ba Huân tiệt trùng", "Hộp", "10 quả", 35000, 31000, "Trứng gà tiệt trùng tia UV sạch khuẩn, lòng đỏ béo ngậy."),
            new(cat2, bBaHuan, "GH-TCT-0009", "893500020009", "Trứng vịt Ba Huân tuyển chọn", "Hộp", "10 quả", 42000, 37000, "Trứng vịt đồng quả lớn, lòng đỏ cam đậm dùng kho thịt ngon hết sẩy."),
            new(cat2, bBaHuan, "GH-TCT-0010", "893500020010", "Trứng cút tươi Ba Huân", "Hộp", "30 quả", 28000, 24000, "Trứng cút sạch vỏ cứng, lòng đỏ béo bùi cho các món kho."),
            new(cat2, null, "GH-TCT-0011", "893500020011", "Cá basa fillet đông lạnh sạch xương", "Túi", "500g", 48000, 42000, "Cá basa phi lê trắng ngần, chiên giòn sốt cà chua thơm ngon."),
            new(cat2, null, "GH-TCT-0012", "893500020012", "Cá thu cắt khúc Phú Quốc", "Khay", "300g", 85000, 75000, "Cá thu khúc chắc thịt, kho riềng hay sốt cà đậm vị quê hương."),
            new(cat2, null, "GH-TCT-0013", "893500020013", "Cá hồi Na Uy tươi fillet", "Khay", "250g", 165000, 148000, "Cá hồi Na Uy giàu Omega-3, áp chảo sốt bơ chanh sang trọng."),
            new(cat2, null, "GH-TCT-0014", "893500020014", "Tôm sú tươi sống Cà Mau", "Hộp", "500g", 185000, 169000, "Tôm sú vỏ dày thịt săn ngọt lịm, hấp bia hoặc nướng muối ớt."),
            new(cat2, null, "GH-TCT-0015", "893500020015", "Tôm thẻ chân trắng tươi lột vỏ", "Khay", "300g", 78000, 68000, "Tôm thẻ sơ chế sạch tiện lợi cho món rim, xào thập cẩm."),
            new(cat2, null, "GH-TCT-0016", "893500020016", "Mực ống tươi biển Phan Thiết", "Khay", "400g", 125000, 109000, "Mực ống dày mình giòn sần sật, hấp gừng hoặc xào dứa."),
            new(cat2, null, "GH-TCT-0017", "893500020017", "Chả cá Nha Trang hấp", "Miếng", "300g", 45000, 39000, "Chả cá quết nhuyễn dai giòn tự nhiên, không hàn the."),
            new(cat2, null, "GH-TCT-0018", "893500020018", "Giò lụa truyền thống lá chuối", "Cây", "500g", 85000, 75000, "Giò lụa lợn ỉ thơm mùi nước mắm cốt và tiêu sọ."),
            new(cat2, null, "GH-TCT-0019", "893500020019", "Thịt thăn bò tươi loại 1", "Khay", "300g", 115000, 99000, "Thịt bò mềm tan không gân, thái lát mỏng xào lăn hay xào bông cải."),
            new(cat2, null, "GH-TCT-0020", "893500020020", "Nạm bò Úc cắt lát lẩu", "Khay", "300g", 98000, 85000, "Nạm bò vân mỡ đều, nhúng lẩu hoặc nấu phở thơm béo ngậy."),
            new(cat2, null, "GH-TCT-0021", "893500020021", "Thịt bò xay tươi nguyên chất", "Khay", "300g", 68000, 59000, "Bò xay tỷ lệ 80/20 dùng làm sốt mì Ý hoặc bò viên thơm ngon."),
            new(cat2, bCp, "GH-TCT-0022", "893500020022", "Xúc xích heo tiệt trùng C.P Red", "Gói", "500g", 58000, 49000, "Xúc xích xông khói vỏ giòn, rán hoặc kẹp bánh mì ăn sáng tiện lợi."),
            new(cat2, null, "GH-TCT-0023", "893500020023", "Ba chỉ bò Mỹ cuộn lẩu nướng", "Khay", "500g", 135000, 119000, "Ba chỉ bò Mỹ cuộn tròn đẹp mắt, nhúng lẩu nấm hoặc nướng BBQ."),
            new(cat2, null, "GH-TCT-0024", "893500020024", "Cá điêu hồng làm sạch nguyên con", "Con", "800g", 65000, 55000, "Cá điêu hồng ngọt thịt ít xương dăm, chiên xù cuốn bánh tráng."),
            new(cat2, null, "GH-TCT-0025", "893500020025", "Lươn đồng rút xương cắt khúc", "Khay", "250g", 75000, 65000, "Lươn đồng chắc thịt xào sả ớt hoặc nấu cháo lươn bổ dưỡng.")
        ]);

        // 3. Sữa & chế phẩm (25 items)
        string cat3 = "sua-che-pham";
        string bVina = "vinamilk";
        string bTh = "th-true-milk";
        items.AddRange([
            new(cat3, bVina, "GH-SCP-0001", "893500030001", "Sữa tươi tiệt trùng Vinamilk có đường", "Hộp", "1L", 38000, 34000, "Sữa tươi 100% sữa bò tươi nguyên chất, bổ sung vitamin D3 và canxi."),
            new(cat3, bVina, "GH-SCP-0002", "893500030002", "Sữa tươi Vinamilk ít đường 1L", "Hộp", "1L", 38000, 34000, "Sữa tươi ít đường ngọt thanh, tốt cho vóc dáng và sức khỏe cả nhà."),
            new(cat3, bTh, "GH-SCP-0003", "893500030003", "Sữa tươi TH True Milk nguyên chất không đường", "Hộp", "1L", 42000, 38000, "Sữa tươi chuẩn sạch từ đồng cỏ trang trại TH, không đường thanh khiết."),
            new(cat3, bTh, "GH-SCP-0004", "893500030004", "Sữa tươi TH True Milk ít đường 1L", "Hộp", "1L", 42000, 38000, "Vị ngọt dịu nhẹ tự nhiên, giữ trọn dưỡng chất tinh túy từ sữa bò tươi."),
            new(cat3, bVina, "GH-SCP-0005", "893500030005", "Lốc 4 hộp sữa tươi Vinamilk có đường 180ml", "Lốc", "4 hộp", 34000, 30000, "Lốc sữa hộp tiện dụng cho bé mang đi học, bổ sung năng lượng nhanh chóng."),
            new(cat3, bTh, "GH-SCP-0006", "893500030006", "Lốc 4 hộp sữa tươi TH True Milk 180ml", "Lốc", "4 hộp", 37000, 33000, "Sữa hộp tiện lợi thơm ngậy mùi sữa tươi trang trại sạch."),
            new(cat3, bVina, "GH-SCP-0007", "893500030007", "Sữa đặc có đường Ông Thọ nắp bật lon đỏ", "Lon", "380g", 27000, 24000, "Sữa đặc Ông Thọ sánh đặc béo ngọt, pha cà phê sữa thơm nức tiếng."),
            new(cat3, bVina, "GH-SCP-0008", "893500030008", "Sữa chua ăn có đường Vinamilk men sống", "Lốc", "4 hộp", 28000, 24000, "Sữa chua lên men tự nhiên, tốt cho hệ tiêu hóa cả gia đình."),
            new(cat3, bVina, "GH-SCP-0009", "893500030009", "Sữa chua uống men sống Probi dâu", "Lốc", "5 chai", 35000, 31000, "Chứa 65 tỷ lợi khuẩn L.Casey giúp tăng cường đề kháng đường ruột."),
            new(cat3, bTh, "GH-SCP-0010", "893500030010", "Bơ lạt tự nhiên TH True Butter 200g", "Hộp", "200g", 62000, 55000, "Bơ lạt làm từ chất béo sữa bò tươi, thơm lừng dùng làm bánh hay nấu ăn."),
            new(cat3, null, "GH-SCP-0011", "893500030011", "Phô mai Con Bò Cười truyền thống hộp 8 miếng", "Hộp", "8 miếng", 42000, 37000, "Phô mai tam giác mềm mịn béo ngậy, giàu kẽm và canxi cho trẻ nhỏ."),
            new(cat3, null, "GH-SCP-0012", "893500030012", "Váng sữa Monte hương vani lốc 4 hộp", "Lốc", "4 hộp", 68000, 59000, "Váng sữa nhập khẩu Đức thơm béo ngậy, món ăn vặt yêu thích của bé."),
            new(cat3, null, "GH-SCP-0013", "893500030013", "Sữa tươi thanh trùng Ba Vì chai 900ml", "Chai", "900ml", 45000, 39000, "Sữa thanh trùng giữ nguyên vẹn vị béo ngậy và hương thơm tự nhiên."),
            new(cat3, bTh, "GH-SCP-0014", "893500030014", "Sữa hạt óc chó TH True Nut lốc 4 hộp", "Lốc", "4 hộp", 48000, 42000, "Sữa hạt dinh dưỡng tự nhiên ngọt từ quả chà là, tốt cho tim mạch."),
            new(cat3, null, "GH-SCP-0015", "893500030015", "Sữa chua nếp cẩm Ba Vì hũ 110g", "Hũ", "110g", 14000, 12000, "Sữa chua kết hợp nếp cẩm dẻo bùi lên men truyền thống."),
            new(cat3, null, "GH-SCP-0016", "893500030016", "Sữa đậu nành Fami nguyên chất lốc 6 hộp 200ml", "Lốc", "6 hộp", 29000, 25000, "100% đạm đậu nành chọn lọc tự nhiên, thơm ngon mát lành."),
            new(cat3, null, "GH-SCP-0017", "893500030017", "Kem tươi Whipping Cream Anchor hộp 250ml", "Hộp", "250ml", 78000, 69000, "Kem béo động vật cao cấp đánh bông mịn màng làm bánh kem."),
            new(cat3, bVina, "GH-SCP-0018", "893500030018", "Sữa chua uống mè đen Vinamilk lốc 4 chai", "Lốc", "4 chai", 30000, 26000, "Bổ sung mè đen thơm bùi, đẹp da giữ dáng cho phụ nữ."),
            new(cat3, null, "GH-SCP-0019", "893500030019", "Phô mai lát Cheddar Anchor gói 200g", "Gói", "12 lát", 75000, 66000, "Phô mai lát mỏng kẹp bánh mì burger nướng chảy béo ngậy."),
            new(cat3, bVina, "GH-SCP-0020", "893500030020", "Sữa đặc Phương Nam xanh hộp 380g", "Lon", "380g", 21000, 18000, "Sữa đặc kinh tế dùng pha chế trà sữa, làm yaourt tại nhà."),
            new(cat3, bVina, "GH-SCP-0021", "893500030021", "Sữa chua Hy Lạp Vinamilk Greek Yogurt", "Hộp", "100g", 18000, 15000, "Sữa chua Hy Lạp sánh mịn giàu đạm, lý tưởng cho người ăn kiêng."),
            new(cat3, null, "GH-SCP-0022", "893500030022", "Sữa tiệt trùng Cô Gái Hà Lan 1L", "Hộp", "1L", 36000, 32000, "Công thức sữa chuẩn Hà Lan dồi dào canxi cho cả gia đình."),
            new(cat3, bTh, "GH-SCP-0023", "893500030023", "Sữa hạt Macca TH True Nut lốc 4 hộp", "Lốc", "4 hộp", 48000, 42000, "Hạt macca cao cấp kết hợp sữa tươi sạch TH."),
            new(cat3, null, "GH-SCP-0024", "893500030024", "Bơ thực vật Margarine Tường An hộp 200g", "Hộp", "200g", 18000, 15000, "Bơ thực vật phết bánh mì nướng bơ tỏi thơm lừng hấp dẫn."),
            new(cat3, null, "GH-SCP-0025", "893500030025", "Sữa lúa mạch Milo nắp bật lốc 4 hộp 180ml", "Lốc", "4 hộp", 32000, 28000, "Chiết xuất mầm lúa mạch bổ sung năng lượng cho ngày năng động.")
        ]);

        // 4. Đồ khô & gia vị (25 items)
        string cat4 = "do-kho-gia-vi";
        string bChinsu = "chin-su";
        string bNamNgu = "nam-ngu";
        string bMaggi = "maggi";
        string bKnorr = "knorr";
        string bBienHoa = "bien-hoa";
        string bSimply = "simply";
        string bTuongAn = "tuong-an";
        string bHaoHao = "hao-hao";
        string bOmachi = "omachi";
        items.AddRange([
            new(cat4, null, "GH-DKG-0001", "893500040001", "Gạo ngon ST25 tiến vua chuẩn vị bao 5kg", "Bao", "5kg", 185000, 165000, "Gạo ngon nhất thế giới, hạt thon dài dẻo ngọt thơm mùi lá dứa."),
            new(cat4, null, "GH-DKG-0002", "893500040002", "Gạo thơm Jasmine thượng hạng bao 5kg", "Bao", "5kg", 135000, 119000, "Gạo thơm dẻo vừa, hạt cơm bóng bẩy đậm đà bữa cơm gia đình."),
            new(cat4, bNamNgu, "GH-DKG-0003", "893500040003", "Nước mắm Nam Ngư Đệ Nhị chai 900ml", "Chai", "900ml", 36000, 31000, "Nước mắm cá cơm thơm dịu hài hòa, chấm hay nấu đều tròn vị."),
            new(cat4, bChinsu, "GH-DKG-0004", "893500040004", "Nước mắm Chin-su cá cơm thượng hạng 500ml", "Chai", "500ml", 48000, 42000, "Hương vị cá cơm tươi nguyên chất thơm lừng mâm cơm Việt."),
            new(cat4, bMaggi, "GH-DKG-0005", "893500040005", "Nước tương Maggi đậm đặc chai 700ml", "Chai", "700ml", 35000, 30000, "Lên men đậu nành tự nhiên, vị đậm đà không hóa chất."),
            new(cat4, bSimply, "GH-DKG-0006", "893500040006", "Dầu đậu nành nguyên chất Simply chai 1L", "Chai", "1L", 65000, 56000, "Dầu ăn thực vật nguyên chất giàu Omega 3-6-9 tốt cho tim mạch."),
            new(cat4, bTuongAn, "GH-DKG-0007", "893500040007", "Dầu thực vật Tường An Cooking Oil chai 1L", "Chai", "1L", 52000, 45000, "Dầu ăn bí quyết giòn ngon cho các món chiên rán vàng ruộm."),
            new(cat4, bKnorr, "GH-DKG-0008", "893500040008", "Hạt nêm Knorr thịt thăn xương ống gói 400g", "Gói", "400g", 38000, 33000, "Hạt nêm từ nước hầm xương nguyên chất, ngọt sâu vị tự nhiên."),
            new(cat4, bMaggi, "GH-DKG-0009", "893500040009", "Hạt nêm Maggi nấm hương thanh ngọt gói 450g", "Gói", "450g", 39000, 34000, "Chiết xuất nấm hương thanh đạm, phù hợp nấu món chay lẫn mặn."),
            new(cat4, bBienHoa, "GH-DKG-0010", "893500040010", "Đường tinh luyện Biên Hòa Pure gói 1kg", "Gói", "1kg", 32000, 28000, "Đường mía tinh khiết trắng ngần, hạt mịn dễ tan nhanh chóng."),
            new(cat4, null, "GH-DKG-0011", "893500040011", "Muối tinh sấy i-ốt Visalco gói 1kg", "Gói", "1kg", 8000, 6000, "Muối sạch tinh khiết bổ sung vi chất i-ốt phòng ngừa bướu cổ."),
            new(cat4, null, "GH-DKG-0012", "893500040012", "Bột ngọt Ajinomoto gói 454g", "Gói", "454g", 36000, 32000, "Gia vị điều vị món ăn thơm ngon hài hòa."),
            new(cat4, null, "GH-DKG-0013", "893500040013", "Tiêu đen xay nguyên chất Phú Quốc hũ 100g", "Hũ", "100g", 38000, 32000, "Tiêu chín cay nồng thơm ngát đặc sản đảo ngọc Phú Quốc."),
            new(cat4, bChinsu, "GH-DKG-0014", "893500040014", "Tương ớt Chin-su chai 250g cay đậm vị", "Chai", "250g", 15000, 12000, "Vạn món ngon bùng vị cay nồng cùng tương ớt quốc dân."),
            new(cat4, null, "GH-DKG-0015", "893500040015", "Bột mì đa dụng Meizan cao cấp gói 1kg", "Gói", "1kg", 24000, 20000, "Bột mì trắng mịn làm bánh bông lan, tẩm bột chiên giòn."),
            new(cat4, bHaoHao, "GH-DKG-0016", "893500040016", "Mì Hảo Hảo tôm chua cay thùng 30 gói", "Thùng", "30 gói", 125000, 112000, "Mì ăn liền quốc dân sợi mì dai giòn nước súp chua cay đặc trưng."),
            new(cat4, bOmachi, "GH-DKG-0017", "893500040017", "Mì Omachi sườn hầm ngũ quả lốc 5 gói", "Lốc", "5 gói", 48000, 42000, "Mì khoai tây không lo bị nóng, nước súp sườn hầm thanh ngọt."),
            new(cat4, null, "GH-DKG-0018", "893500040018", "Phở bò Đệ Nhất gói 68g thơm lừng", "Gói", "68g", 9000, 7500, "Bánh phở mềm dai kết hợp nước súp bò hầm hồi quế thơm nức."),
            new(cat4, null, "GH-DKG-0019", "893500040019", "Bún tươi sấy khô Safoco gói 500g", "Gói", "500g", 25000, 21000, "Sợi bún dai ngon như bún tươi sau khi trụng nước sôi."),
            new(cat4, null, "GH-DKG-0020", "893500040020", "Miến dong làng So cao cấp gói 300g", "Gói", "300g", 35000, 29000, "Miến dong nguyên chất nấu lâu không bị nát hay đục nước."),
            new(cat4, null, "GH-DKG-0021", "893500040021", "Bánh tráng chả giò Mikiri gói 30 lá", "Gói", "30 lá", 18000, 14000, "Vỏ bánh tráng mỏng dẻo, cuốn chả giò chiên vàng giòn rụm lâu."),
            new(cat4, null, "GH-DKG-0022", "893500040022", "Nấm hương khô rừng Tây Bắc gói 100g", "Gói", "100g", 55000, 46000, "Nấm hương cánh dày thơm phức, tăng hương vị món xào nấu canh."),
            new(cat4, null, "GH-DKG-0023", "893500040023", "Mộc nhĩ đen khô cạo sạch chân 100g", "Gói", "100g", 30000, 25000, "Mộc nhĩ tai dày giòn sần sật, nguyên liệu làm nem chả."),
            new(cat4, null, "GH-DKG-0024", "893500040024", "Đậu xanh đãi vỏ cao cấp gói 500g", "Gói", "500g", 38000, 32000, "Hạt đậu đều tăm tắp, bùi thơm nấu chè hoặc làm nhân bánh."),
            new(cat4, null, "GH-DKG-0025", "893500040025", "Đậu đen xanh lòng giống thuần gói 500g", "Gói", "500g", 35000, 29000, "Đậu đen xanh lòng nấu nước mát giải nhiệt cơ thể cực tốt.")
        ]);

        // 5. Đồ uống (25 items)
        string cat5 = "do-uong";
        string bLavie = "la-vie";
        string bCoke = "coca-cola";
        string bPepsi = "pepsi";
        items.AddRange([
            new(cat5, bLavie, "GH-DU-0001", "893500050001", "Nước khoáng thiên nhiên La Vie chai 500ml", "Chai", "500ml", 6000, 5000, "Nước khoáng thiên nhiên giàu khoáng chất nhẹ tốt cho cơ thể."),
            new(cat5, null, "GH-DU-0002", "893500050002", "Nước tinh khiết Aquafina chai 500ml", "Chai", "500ml", 6000, 5000, "Nước tinh khiết lọc qua hệ thống thẩm thấu ngược 7 bước hiện đại."),
            new(cat5, bCoke, "GH-DU-0003", "893500050003", "Nước ngọt Coca-Cola lon 320ml chính hãng", "Lon", "320ml", 11000, 9500, "Vị cola nguyên bản sảng khoái đánh tan cơn khát tức thì."),
            new(cat5, bCoke, "GH-DU-0004", "893500050004", "Lốc 6 lon Coca-Cola 320ml", "Lốc", "6 lon", 62000, 54000, "Lốc 6 lon tiện lợi tiết kiệm cho các bữa tiệc gia đình."),
            new(cat5, bPepsi, "GH-DU-0005", "893500050005", "Nước ngọt Pepsi vị chanh không calo lon 320ml", "Lon", "320ml", 11000, 9500, "Pepsi Zero Calorie thanh mát không lo tăng cân."),
            new(cat5, bPepsi, "GH-DU-0006", "893500050006", "Nước ngọt 7Up lon 320ml hương chanh", "Lon", "320ml", 11000, 9500, "Hương chanh tự nhiên tươi mát bùng nổ năng lượng."),
            new(cat5, null, "GH-DU-0007", "893500050007", "Nước tăng lực Red Bull lon vàng 250ml", "Lon", "250ml", 14000, 12000, "Húc tung năng lượng, tỉnh táo tức thì để làm việc hiệu quả."),
            new(cat5, bPepsi, "GH-DU-0008", "893500050008", "Nước tăng lực Sting dâu tây đỏ chai 330ml", "Chai", "330ml", 11000, 9000, "Vị dâu tây ngọt ngào ga sủi lăn tăn sảng khoái."),
            new(cat5, null, "GH-DU-0009", "893500050009", "Trà xanh Không Độ chai 455ml vị chanh", "Chai", "455ml", 11000, 9000, "Chiết xuất lá trà xanh Thái Nguyên giữ trọn EGCG giải tỏa căng thẳng."),
            new(cat5, null, "GH-DU-0010", "893500050010", "Trà Ô Long Tea+ Plus chai 455ml", "Chai", "455ml", 11000, 9500, "Hoạt chất OTPP hạn chế hấp thu chất béo sau bữa ăn no."),
            new(cat5, null, "GH-DU-0011", "893500050011", "Trà sữa đóng chai C2 hương hồng trà 350ml", "Chai", "350ml", 12000, 10000, "Trà sữa thơm ngọt béo thanh mát cho giới trẻ."),
            new(cat5, null, "GH-DU-0012", "893500050012", "Cà phê hòa tan G7 3in1 hộp 18 gói", "Hộp", "18 gói", 62000, 54000, "Cà phê G7 Trung Nguyên đậm đà hương thơm nồng nàn quyến rũ."),
            new(cat5, null, "GH-DU-0013", "893500050013", "Cà phê đen hòa tan G7 hộp 15 gói", "Hộp", "15 gói", 38000, 32000, "Cà phê đen thuần khiết đậm vị đắng thanh kích thích trí não."),
            new(cat5, null, "GH-DU-0014", "893500050014", "Cà phê phin Sáng Tạo 1 Trung Nguyên gói 340g", "Gói", "340g", 75000, 65000, "Hạt Culi Robusta đậm đà vị cà phê phin truyền thống Việt Nam."),
            new(cat5, null, "GH-DU-0015", "893500050015", "Nước cam ép có tép Teppy chai 1L", "Chai", "1L", 28000, 24000, "Nước cam tép giòn sần sật mọng nước thơm ngon bổ dưỡng."),
            new(cat5, null, "GH-DU-0016", "893500050016", "Nước dừa tươi nguyên chất Cocoxim hộp 330ml", "Hộp", "330ml", 18000, 15000, "100% nước dừa tươi Bến Tre ngọt thanh mát lành."),
            new(cat5, null, "GH-DU-0017", "893500050017", "Bia Heineken Silver lon cao 330ml", "Lon", "330ml", 22000, 19000, "Vị bia êm đầm nhẹ dịu dễ uống từ thương hiệu toàn cầu."),
            new(cat5, null, "GH-DU-0018", "893500050018", "Bia Tiger Crystal lon 330ml sảng khoái", "Lon", "330ml", 20000, 17500, "Bia tuyết mát lạnh lọc ở nhiệt độ -1 độ C cực sảng khoái."),
            new(cat5, null, "GH-DU-0019", "893500050019", "Bia Sài Gòn Special lon lùn 330ml", "Lon", "330ml", 17000, 15000, "Bia rồng xanh hạt đại mạch thơm nồng vị đậm đà truyền thống."),
            new(cat5, null, "GH-DU-0020", "893500050020", "Nước yến sào ngân nhĩ Sanest lon 190ml", "Lon", "190ml", 14000, 11500, "Yến sào Khánh Hòa kết hợp ngân nhĩ bổ phổi đẹp da."),
            new(cat5, null, "GH-DU-0021", "893500050021", "Trà Atiso túi lọc Ladophar hộp 100 gói", "Hộp", "100 gói", 95000, 82000, "Trà thảo dược mát gan thanh lọc cơ thể từ hoa atiso Đà Lạt."),
            new(cat5, null, "GH-DU-0022", "893500050022", "Trà sencha gạo lứt rang túi lọc 50 gói", "Hộp", "50 gói", 65000, 56000, "Hương thơm gạo lứt bùi ngậy quyện vị trà xanh thanh tịnh."),
            new(cat5, null, "GH-DU-0023", "893500050023", "Cà phê hòa tan Vinacafe Chất 3in1 bịch 24 gói", "Bịch", "24 gói", 72000, 62000, "Hương vị cà phê nguyên bản đậm chất người sành uống."),
            new(cat5, null, "GH-DU-0024", "893500050024", "Nước chanh muối bù khoáng Restore chai 500ml", "Chai", "500ml", 11000, 9000, "Chanh muối lên men tự nhiên bổ sung điện giải khi vận động."),
            new(cat5, bVina, "GH-DU-0025", "893500050025", "Nước ép táo Vfresh nguyên chất hộp 1L", "Hộp", "1L", 48000, 42000, "100% nước ép táo tự nhiên không bổ sung đường hóa học.")
        ]);

        // 6. Bánh kẹo & ăn vặt (25 items)
        string cat6 = "banh-keo-an-vat";
        string bOrion = "orion";
        string bKinhDo = "kinh-do";
        items.AddRange([
            new(cat6, bOrion, "GH-BKV-0001", "893500060001", "Bánh ChocoPie Orion hộp 12 cái 396g", "Hộp", "12 cái", 58000, 50000, "Bánh socola mềm thơm kẹp marshmallow dẻo dai béo ngậy."),
            new(cat6, bKinhDo, "GH-BKV-0002", "893500060002", "Bánh quy AFC dinh dưỡng vị rau cải hộp 200g", "Hộp", "200g", 32000, 27000, "Bánh quy giòn rụm bổ sung chất xơ và canxi ăn vặt không lo béo."),
            new(cat6, null, "GH-BKV-0003", "893500060003", "Bánh snack Oishi tôm cay gói 40g", "Gói", "40g", 7000, 5500, "Snack tôm cay giòn tan thơm nức vị cay hấp dẫn tuổi thơ."),
            new(cat6, null, "GH-BKV-0004", "893500060004", "Snack bắp ngọt Oishi Corn Snack gói 40g", "Gói", "40g", 7000, 5500, "Bắp ngô vàng ngọt giòn thơm nức mũi."),
            new(cat6, null, "GH-BKV-0005", "893500060005", "Snack khoai tây Lay's tự nhiên gói 56g", "Gói", "56g", 15000, 12000, "Khoai tây chiên lát mỏng giòn rụm nguyên bản số 1 thế giới."),
            new(cat6, bKinhDo, "GH-BKV-0006", "893500060006", "Bánh quy Cosy mè Kinh Đô gói 300g", "Gói", "300g", 34000, 29000, "Mè rang giòn thơm bùi hòa quyện vỏ bánh quy bơ béo."),
            new(cat6, bKinhDo, "GH-BKV-0007", "893500060007", "Bánh bông lan Solite trứng sữa hộp 10 cái", "Hộp", "10 cái", 42000, 36000, "Vỏ bánh bông lan mềm xốp nhân kem trứng sữa ngọt ngào."),
            new(cat6, null, "GH-BKV-0008", "893500060008", "Kẹo dẻo Chupa Chups hương trái cây gói 90g", "Gói", "90g", 18000, 15000, "Kẹo dẻo chua ngọt hình gấu xinh xắn thơm mát hương cam dâu."),
            new(cat6, null, "GH-BKV-0009", "893500060009", "Kẹo ngậm Vitamin C Hải Hà gói 100g", "Gói", "100g", 12000, 9500, "Kẹo ngậm hương cam thơm bổ sung vitamin C tăng sức đề kháng."),
            new(cat6, null, "GH-BKV-0010", "893500060010", "Hạt điều rang muối Bình Phước hũ 250g", "Hũ", "250g", 95000, 82000, "Hạt điều nguyên hạt loại A giòn bùi ngậy đậm đà vị muối."),
            new(cat6, null, "GH-BKV-0011", "893500060011", "Hạt dẻ cười Mỹ nứt vỏ hũ 250g", "Hũ", "250g", 115000, 99000, "Hạt dẻ cười hạt to tròn vị béo ngậy tốt cho sức khỏe tim mạch."),
            new(cat6, null, "GH-BKV-0012", "893500060012", "Rong biển tẩm gia vị Tao Kae Noi gói 32g", "Gói", "32g", 25000, 21000, "Rong biển nướng giòn rụm đậm vị cay truyền thống Thái Lan."),
            new(cat6, null, "GH-BKV-0013", "893500060013", "Khô gà lá chanh cay cay hũ 200g", "Hũ", "200g", 65000, 55000, "Thịt ức gà xé sợi tẩm ướp lá chanh thơm cay nồng nàn."),
            new(cat6, null, "GH-BKV-0014", "893500060014", "Khô bò sợi đặc sản Đà Nẵng hũ 200g", "Hũ", "200g", 135000, 118000, "Thịt bò tươi tẩm ướp ớt tiêu đậm đà làm mồi nhắm bia số một."),
            new(cat6, null, "GH-BKV-0015", "893500060015", "Bánh tráng trộn khô gà Tây Ninh bịch lớn", "Bịch", "150g", 25000, 20000, "Bánh tráng phơi sương muối tôm hành phi sa tế cay ngon."),
            new(cat6, null, "GH-BKV-0016", "893500060016", "Bánh quy bơ Danisa hộp thiếc 454g", "Hộp", "454g", 145000, 128000, "Bánh quy bơ công thức hoàng gia Đan Mạch sang trọng biếu tặng."),
            new(cat6, bOrion, "GH-BKV-0017", "893500060017", "Bánh gạo nướng An vị tảo biển gói 111g", "Gói", "111g", 22000, 18000, "Bánh gạo 100% gạo Nhật Japonica nướng giòn vị tảo biển thanh nhẹ."),
            new(cat6, null, "GH-BKV-0018", "893500060018", "Bánh gạo One One vị phô mai ngô gói 150g", "Gói", "150g", 28000, 24000, "Vị phô mai bắp thơm béo giòn tan từng miếng bánh gạo."),
            new(cat6, null, "GH-BKV-0019", "893500060019", "Kẹo thanh sôcôla sữa KitKat thanh 4 ngón", "Thanh", "35g", 14000, 11000, "Bánh xốp giòn phủ sô cô la sữa ngọt ngào xả stress tức thì."),
            new(cat6, null, "GH-BKV-0020", "893500060020", "Bánh xốp phô mai Nabati hộp 300g", "Hộp", "300g", 36000, 30000, "Bánh xốp giòn tan ngập tràn lớp kem phô mai béo ngậy."),
            new(cat6, null, "GH-BKV-0021", "893500060021", "Cơm cháy chà bông cay giòn Sài Gòn 200g", "Bịch", "200g", 45000, 38000, "Cơm cháy đáy nồi giòn rụm phủ lớp chà bông thơm cay hấp dẫn."),
            new(cat6, null, "GH-BKV-0022", "893500060022", "Mực Bento cay ngọt Thái Lan gói 20g", "Gói", "20g", 14000, 11500, "Mực nướng sấy gia vị cay xé lưỡi ăn vặt cực cuốn."),
            new(cat6, null, "GH-BKV-0023", "893500060023", "Kẹo dừa Bến Tre truyền thống gói 300g", "Gói", "300g", 35000, 29000, "Đặc sản kẹo dừa sầu riêng dẻo ngọt béo bùi vị quê hương."),
            new(cat6, null, "GH-BKV-0024", "893500060024", "Bánh gấu nhân kem sữa hộp 250g", "Hộp", "250g", 38000, 32000, "Vỏ bánh gòn rụm hình chú gấu bọc nhân sữa thơm ngọt."),
            new(cat6, null, "GH-BKV-0025", "893500060025", "Bánh quy bơ trứng Tipo gói 220g", "Gói", "220g", 32000, 27000, "Bánh trứng nướng vàng giòn thơm nức mũi mùi bơ tươi.")
        ]);

        // 7. Chăm sóc cá nhân (25 items)
        string cat7 = "cham-soc-ca-nhan";
        string bSunsilk = "sunsilk";
        string bLifebuoy = "lifebuoy";
        items.AddRange([
            new(cat7, bSunsilk, "GH-CSCN-0001", "893500070001", "Dầu gội Sunsilk óng mượt rạng ngời chai 650g", "Chai", "650g", 145000, 128000, "Chiết xuất bồ kết và dầu hoa trà dưỡng tóc đen mượt óng ả."),
            new(cat7, null, "GH-CSCN-0002", "893500070002", "Dầu gội Clear sạch gàu mát lạnh bạc hà 630g", "Chai", "630g", 165000, 145000, "Công nghệ sạch gàu số 1 cùng tinh chất bạc hà the mát tột cùng."),
            new(cat7, null, "GH-CSCN-0003", "893500070003", "Dầu gội Head & Shoulders bạc hà chai 625ml", "Chai", "625ml", 175000, 155000, "Đánh bay vảy gàu ngứa ngáy, mang lại da đầu khỏe khoắn."),
            new(cat7, null, "GH-CSCN-0004", "893500070004", "Dầu xả Pantene suôn mượt óng ả tuýp 300ml", "Tuýp", "300ml", 85000, 75000, "Dưỡng chất Pro-V phục hồi tóc hư tổn chẻ ngọn mềm mượt."),
            new(cat7, bLifebuoy, "GH-CSCN-0005", "893500070005", "Sữa tắm Lifebuoy bảo vệ vượt trội chai 850g", "Chai", "850g", 168000, 149000, "Kháng khuẩn vượt trội 99.9% bảo vệ làn da cả gia đình."),
            new(cat7, null, "GH-CSCN-0006", "893500070006", "Sữa tắm dưỡng ẩm Purité hoa anh đào chai 850ml", "Chai", "850ml", 185000, 162000, "Hương thơm ngọt ngào thư giãn như lạc giữa rừng hoa anh đào Pháp."),
            new(cat7, bLifebuoy, "GH-CSCN-0007", "893500070007", "Xà bông cục Lifebuoy bảo vệ vượt trội 90g", "Cục", "90g", 16000, 13000, "Xà phòng bánh kháng khuẩn sạch thơm bảo vệ đôi tay sạch sẽ."),
            new(cat7, null, "GH-CSCN-0008", "893500070008", "Kem đánh răng Closeup bạc hà the mát tuýp 230g", "Tuýp", "230g", 45000, 39000, "Gel xanh trong suốt mang lại hơi thở thơm mát tự tin suốt ngày dài."),
            new(cat7, null, "GH-CSCN-0009", "893500070009", "Kem đánh răng Colgate ngừa sâu răng tối ưu 225g", "Tuýp", "225g", 42000, 36000, "Bổ sung Canxi và Flouride bảo vệ men răng chắc khỏe."),
            new(cat7, null, "GH-CSCN-0010", "893500070010", "Bàn chải đánh răng Colgate lông tơ than hoạt tính", "Cây", "1 cây", 32000, 26000, "Sợi lông chải siêu mảnh len lỏi làm sạch từng kẽ răng nhỏ nhất."),
            new(cat7, null, "GH-CSCN-0011", "893500070011", "Nước súc miệng Listerine Cool Mint chai 500ml", "Chai", "500ml", 98000, 85000, "Diệt sạch 99.9% vi khuẩn gây hôi miệng và mảng bám."),
            new(cat7, null, "GH-CSCN-0012", "893500070012", "Khăn ướt em bé không mùi Bobby gói 80 miếng", "Gói", "80 miếng", 38000, 32000, "Không cồn không paraben, an toàn tuyệt đối cho làn da bé sơ sinh."),
            new(cat7, null, "GH-CSCN-0013", "893500070013", "Băng vệ sinh Kotex khô thoáng cánh gói 8 miếng", "Gói", "8 miếng", 22000, 18000, "Màng đáy thấm hút nhanh chóng, tạo cảm giác khô thoáng dịu êm."),
            new(cat7, null, "GH-CSCN-0014", "893500070014", "Bọt cạo râu Gillette hương chanh chai 175g", "Chai", "175g", 75000, 64000, "Lớp bọt xốp mịn màng làm mềm râu giúp cạo êm dịu không rát da."),
            new(cat7, null, "GH-CSCN-0015", "893500070015", "Dao cạo râu Gillette Vector đầu xoay", "Cây", "1 cây", 52000, 44000, "Lưỡi dao kép tự điều chỉnh theo đường cong khuôn mặt."),
            new(cat7, null, "GH-CSCN-0016", "893500070016", "Sữa rửa mặt ngừa mụn Acnes tuýp 100g", "Tuýp", "100g", 68000, 59000, "Công thức Trio-ACTIV kháng khuẩn, sạch nhờn và ngăn ngừa mụn."),
            new(cat7, null, "GH-CSCN-0017", "893500070017", "Lăn khử mùi Nivea ngọc trai sáng mịn chai 50ml", "Chai", "50ml", 78000, 68000, "Khử mùi hiệu quả suốt 48h cho vùng da dưới cánh tay mịn màng."),
            new(cat7, null, "GH-CSCN-0018", "893500070018", "Xịt khử mùi toàn thân X-Men For Boss chai 150ml", "Chai", "150ml", 95000, 82000, "Hương nước hoa trầm ấm nam tính quyến rũ chuẩn phong cách quý ông."),
            new(cat7, null, "GH-CSCN-0019", "893500070019", "Dung dịch vệ sinh phụ nữ Dạ Hương chai 120ml", "Chai", "120ml", 36000, 30000, "Chiết xuất thảo dược cúc la mã lô hội dịu nhẹ thanh khiết."),
            new(cat7, null, "GH-CSCN-0020", "893500070020", "Bông tẩy trang cotton Ipek Thổ Nhĩ Kỳ 150 miếng", "Cây", "150 miếng", 42000, 35000, "100% bông cotton mềm mại không xơ dính dơ trên mặt."),
            new(cat7, null, "GH-CSCN-0021", "893500070021", "Nước tẩy trang Garnier Micellar làm sạch sâu 400ml", "Chai", "400ml", 165000, 145000, "Công nghệ micelle hút sạch cặn trang điểm và dầu thừa nhẹ nhàng."),
            new(cat7, null, "GH-CSCN-0022", "893500070022", "Kem chống nắng Skin Aqua Sunplay tuýp 50g", "Tuýp", "50g", 115000, 99000, "Chống nắng phổ rộng SPF50+ PA++++ nâng tông da tự nhiên rạng rỡ."),
            new(cat7, null, "GH-CSCN-0023", "893500070023", "Tăm bông kháng khuẩn thân trúc Merita hộp 200 que", "Hộp", "200 que", 18000, 14000, "Đầu bông tiệt trùng êm ái dùng vệ sinh tai và trang điểm."),
            new(cat7, null, "GH-CSCN-0024", "893500070024", "Dầu gội nam Romano Classic sạch gàu chai 650g", "Chai", "650g", 155000, 138000, "Mùi hương cổ điển nam tính đẳng cấp dành cho phái mạnh."),
            new(cat7, bLifebuoy, "GH-CSCN-0025", "893500070025", "Gel rửa tay khô sát khuẩn Lifebuoy chai 100ml", "Chai", "100ml", 35000, 29000, "Rửa tay sạch khuẩn tức thì không cần nước, nhỏ gọn tiện mang theo.")
        ]);

        // 8. Vệ sinh nhà cửa (25 items)
        string cat8 = "ve-sinh-nha-cua";
        string bOmo = "omo";
        string bSunlight = "sunlight";
        string bVim = "vim";
        string bCif = "cif";
        items.AddRange([
            new(cat8, bOmo, "GH-VSN-0001", "893500080001", "Nước giặt Omo Matic khử mùi ẩm mốc túi 3.6kg", "Túi", "3.6kg", 215000, 189000, "Công nghệ xoáy sâu đánh bay vết bẩn cứng đầu và khử mùi hôi ẩm mốc."),
            new(cat8, null, "GH-VSN-0002", "893500080002", "Nước giặt Ariel hương Downy đam mê túi 3.2kg", "Túi", "3.2kg", 205000, 179000, "Giặt sạch tinh tươm lưu hương nước hoa quý phái dài lâu trên sợi vải."),
            new(cat8, null, "GH-VSN-0003", "893500080003", "Nước xả vải Downy hương hoa nắng túi 3L", "Túi", "3L", 185000, 162000, "Lưu hương thơm ngát suốt ngày dài, cho áo quần mềm mại như mới."),
            new(cat8, null, "GH-VSN-0004", "893500080004", "Nước xả vải Comfort tinh dầu thơm túi 3.2L", "Túi", "3.2L", 185000, 162000, "Hương thơm quý phái sang trọng chống tĩnh điện cho sợi vải."),
            new(cat8, bSunlight, "GH-VSN-0005", "893500080005", "Nước rửa chén Sunlight chanh chai 750g", "Chai", "750g", 32000, 27000, "Đánh bay dầu mỡ cứng đầu chỉ trong một lần quẹt, dịu nhẹ da tay."),
            new(cat8, null, "GH-VSN-0006", "893500080006", "Nước rửa chén Mỹ Hảo hương chanh đậm đặc 750ml", "Chai", "750ml", 26000, 22000, "Bọt mịn đậm đặc sạch bóng kin kít bát đĩa gia đình."),
            new(cat8, bSunlight, "GH-VSN-0007", "893500080007", "Nước lau sàn Sunlight hương hoa hạ chai 1kg", "Chai", "1kg", 36000, 30000, "Sàn nhà sáng bóng sạch bong kin kít hương hoa thơm ngát cả gian nhà."),
            new(cat8, bCif, "GH-VSN-0008", "893500080008", "Nước lau kính Cif hương trà xanh chai 520ml", "Chai", "520ml", 28000, 23000, "Lau kính trong suốt không để lại vệt mờ hay mùi hắc khó chịu."),
            new(cat8, bVim, "GH-VSN-0009", "893500080009", "Nước tẩy bồn cầu Vim diệt khuẩn đậm đặc 900ml", "Chai", "900ml", 39000, 33000, "Tẩy sạch vết ố vàng cặn bẩn, diệt khuẩn bồn cầu sạch tinh tươm."),
            new(cat8, null, "GH-VSN-0010", "893500080010", "Nước tẩy trắng quần áo Javel chai 1L", "Chai", "1L", 22000, 18000, "Đánh bay vết ố vàng mốc meo trên quần áo trắng sạch như mới."),
            new(cat8, null, "GH-VSN-0011", "893500080011", "Xịt diệt côn trùng Raid hương cam chai 600ml", "Chai", "600ml", 78000, 68000, "Tiêu diệt muỗi gián ruồi nhanh gọn, an toàn hương cam dịu nhẹ."),
            new(cat8, null, "GH-VSN-0012", "893500080012", "Sáp thơm phòng Glade hương oải hương hộp 180g", "Hộp", "180g", 48000, 40000, "Tỏa hương thơm lavender thư giãn xua tan mùi ẩm mốc phòng ngủ."),
            new(cat8, null, "GH-VSN-0013", "893500080013", "Khăn giấy lau bếp Pulppy lốc 2 cuộn lớn", "Lốc", "2 cuộn", 36000, 30000, "Giấy dai thấm hút dầu mỡ cực tốt không bị rách khi thấm nước."),
            new(cat8, null, "GH-VSN-0014", "893500080014", "Giấy vệ sinh lụa cao cấp Pulppy lốc 10 cuộn", "Lốc", "10 cuộn", 85000, 72000, "Giấy vệ sinh 3 lớp mềm mại mịn màng không chứa chất huỳnh quang."),
            new(cat8, null, "GH-VSN-0015", "893500080015", "Màng bọc thực phẩm PE Sunhouse cuộn 30m", "Hộp", "30m", 32000, 26000, "Chất liệu nhựa PE an toàn giữ trọn độ tươi ngon cho thực phẩm."),
            new(cat8, null, "GH-VSN-0016", "893500080016", "Túi rác tự hủy sinh học 3 màu cuộn 1kg", "Cuộn", "1kg", 45000, 38000, "Túi rác tự phân hủy thân thiện môi trường dai chắc không rách."),
            new(cat8, null, "GH-VSN-0017", "893500080017", "Miếng rửa chén bọt biển kháng khuẩn 3M Scotch-Brite", "Vỉ", "3 miếng", 28000, 23000, "Cọ sạch vết cháy khét mà không làm xước chảo chống dính cao cấp."),
            new(cat8, null, "GH-VSN-0018", "893500080018", "Bùi nhùi thép cọ xoong nồi inox set 2 cái", "Gói", "2 cái", 15000, 12000, "Sợi thép không gỉ đánh bay vết rỉ sét ố đen đáy xoong nồi."),
            new(cat8, null, "GH-VSN-0019", "893500080019", "Găng tay cao su gia dụng size M đôi", "Đôi", "1 đôi", 22000, 18000, "Cao su tự nhiên mềm dẻo bảo vệ da tay khi dọn dẹp vệ sinh."),
            new(cat8, null, "GH-VSN-0020", "893500080020", "Cây cọ bồn cầu cán dài tiện dụng", "Cây", "1 cây", 35000, 28000, "Lông cọ cứng cáp đánh bay mọi ngóc ngách cặn bẩn bồn cầu."),
            new(cat8, null, "GH-VSN-0021", "893500080021", "Bộ cây lau nhà xoay 360 độ Lock&Lock", "Bộ", "1 bộ", 380000, 329000, "Thùng vắt trợ lực ly tâm nhẹ nhàng lau sạch mọi ngóc ngách."),
            new(cat8, null, "GH-VSN-0022", "893500080022", "Xịt phòng khử mùi hương hoa sen chai 280ml", "Chai", "280ml", 42000, 35000, "Khử mùi thức ăn thuốc lá ẩm mốc mang lại không gian trong lành."),
            new(cat8, bCif, "GH-VSN-0023", "893500080023", "Kem tẩy đa năng Cif hương chanh chai 500ml", "Chai", "500ml", 38000, 32000, "Lau sạch vết dầu mỡ bếp ga, bồn rửa chén và bề mặt inox."),
            new(cat8, null, "GH-VSN-0024", "893500080024", "Viên tẩy lồng máy giặt khử khuẩn hộp 12 viên", "Hộp", "12 viên", 45000, 38000, "Tẩy sạch cặn canxi mảng bám cặn bột giặt trong lồng giặt."),
            new(cat8, null, "GH-VSN-0025", "893500080025", "Nước tẩy cặn canxi vòi sen Smart Cleaner 500ml", "Chai", "500ml", 85000, 72000, "Phục hồi bề mặt inox vòi hoa sen sáng bóng như mới sau 5 phút.")
        ]);

        return items;
    }

    private sealed record SampleProductItem(
        string CategorySlug,
        string? BrandSlug,
        string Sku,
        string Barcode,
        string Name,
        string Unit,
        string PackSize,
        long ListPrice,
        long SellingPrice,
        string ShortDescription);
}
