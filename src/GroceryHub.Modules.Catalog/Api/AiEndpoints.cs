using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GroceryHub.Modules.Catalog.Api;

public sealed record AiChatQueryRequest(
    string Message,
    string? Model = null,
    string? Endpoint = null,
    string? ApiKey = null,
    string? Provider = null);

public sealed record AiChatQueryResponse(
    string Reply,
    string? Model = null,
    bool Success = true);

public static class AiEndpoints
{
    private const string BaseStoreInfo = """
        Bạn là Trợ lý ảo AI thông minh và thân thiện của siêu thị thực phẩm tươi sạch GroceryHub Vietnam.
        Thông tin cửa hàng:
        - Địa chỉ cửa hàng: Thôn 9, Xã Suối Hai, Huyện Ba Vì, TP. Hà Nội.
        - Hotline / Zalo hỗ trợ: 0865 087 784
        - Email: dangbinhduong.work@gmail.com
        - Phạm vi giao hàng: Giao hàng tận nơi tới BẤT KỲ ĐỊA CHỈ NÀO trên toàn quốc (giao hỏa tốc nội thành và chuyển phát bảo quản tươi sạch toàn quốc).
        - Chính sách: Miễn phí vận chuyển cho đơn hàng từ 300.000 ₫, cam kết đổi trả miễn phí trong 24 giờ nếu hàng không tươi ngon.
        - Thanh toán: Chuyển khoản VietQR Napas 24/7 (khớp tự động 3 giây) hoặc Tiền mặt khi nhận hàng (COD).
        """;

    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/ai/chat", async (
            [FromBody] AiChatQueryRequest request,
            GroceryHubDbContext dbContext,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            TimeProvider timeProvider,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Results.BadRequest(new { error = "Nội dung tin nhắn không được để trống.", success = false });
            }

            string apiKey = !string.IsNullOrWhiteSpace(request.ApiKey)
                ? request.ApiKey.Trim()
                : configuration["AI:ApiKey"]
                  ?? configuration["VITE_AI_API_KEY"]
                  ?? configuration["AI__ApiKey"]
                  ?? string.Empty;

            string model = !string.IsNullOrWhiteSpace(request.Model)
                ? request.Model.Trim()
                : configuration["AI:Model"]
                  ?? configuration["VITE_AI_MODEL"]
                  ?? configuration["AI__Model"]
                  ?? "gpt-6.1-sol";

            string endpoint = !string.IsNullOrWhiteSpace(request.Endpoint)
                ? request.Endpoint.Trim()
                : configuration["AI:Endpoint"]
                  ?? configuration["VITE_AI_ENDPOINT"]
                  ?? configuration["AI__Endpoint"]
                  ?? "https://api.xingchiapi.com/v1/chat/completions";

            // Normalize endpoint URL: ensure it reaches /v1/chat/completions
            endpoint = endpoint.TrimEnd('/');
            if (!endpoint.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
            {
                if (endpoint.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
                {
                    endpoint += "/chat/completions";
                }
                else
                {
                    endpoint += "/v1/chat/completions";
                }
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return Results.BadRequest(new { error = "Chưa cấu hình API Key cho Trợ lý AI.", success = false });
            }

            try
            {
                // Dynamic live catalog context from database
                string storeKnowledge = await BuildStoreKnowledgeContextAsync(dbContext, request.Message, timeProvider, ct);

                string fullSystemPrompt = $"""
                    {BaseStoreInfo}

                    {storeKnowledge}

                    QUY TẮC TƯ VẤN VÀ GIỚI THIỆU SẢN PHẨM:
                    1. BẮT BUỘC sử dụng các sản phẩm ĐANG CÓ SẴN trong danh sách sản phẩm ở trên khi khách hàng hỏi về hàng hóa, gợi ý món ăn hay thực đơn.
                    2. Với mỗi sản phẩm giới thiệu, hãy nêu rõ: **Tên sản phẩm**, **Giá bán** (VND) và **Đơn vị tính** (ví dụ: 48.000 ₫/Kg), đồng thời chèn link Markdown dẫn tới sản phẩm dạng [Tên sản phẩm](/product/slug).
                    3. Nếu khách hàng hỏi một món siêu thị không có sẵn, hãy khéo léo thông báo và gợi ý các món tương tự đang có sẵn tại quầy.
                    4. Khi khách hỏi về mã giảm giá / khuyến mãi, hãy báo các mã đang có hiệu lực ở trên.
                    5. Luôn trả lời ngắn gọn, lịch sự, ân cần, định dạng Markdown đẹp mắt (in đậm, gạch đầu dòng) bằng tiếng Việt tự nhiên.
                    """;

                HttpClient client = httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(60);

                using HttpRequestMessage httpRequest = new(HttpMethod.Post, endpoint);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                // Programming client User-Agent required by XingChi API
                httpRequest.Headers.TryAddWithoutValidation("User-Agent", "Claude-Code/1.0");

                var payload = new
                {
                    model,
                    messages = new object[]
                    {
                        new { role = "system", content = fullSystemPrompt },
                        new { role = "user", content = request.Message }
                    },
                    temperature = 0.7
                };

                httpRequest.Content = JsonContent.Create(payload);

                using HttpResponseMessage httpResponse = await client.SendAsync(httpRequest, ct);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    string errBody = await httpResponse.Content.ReadAsStringAsync(ct);
                    return Results.Json(new
                    {
                        error = $"API trả về lỗi HTTP {(int)httpResponse.StatusCode}: {errBody}",
                        status = (int)httpResponse.StatusCode,
                        success = false
                    }, statusCode: (int)httpResponse.StatusCode);
                }

                using JsonDocument jsonDoc = await JsonDocument.ParseAsync(
                    await httpResponse.Content.ReadAsStreamAsync(ct),
                    cancellationToken: ct);

                string reply = jsonDoc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? string.Empty;

                return Results.Ok(new AiChatQueryResponse(reply, model, Success: true));
            }
            catch (Exception ex)
            {
                return Results.Json(new
                {
                    error = $"Lỗi kết nối máy chủ AI: {ex.Message}",
                    success = false
                }, statusCode: StatusCodes.Status502BadGateway);
            }
        })
        .RequireAuthorization()
        .WithName("AiChatQuery")
        .WithTags("AIChat");

        return endpoints;
    }

    private static async Task<string> BuildStoreKnowledgeContextAsync(
        GroceryHubDbContext dbContext,
        string userMessage,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        StringBuilder sb = new();

        // 1. Categories
        List<string> categories = await dbContext.Categories
            .AsNoTracking()
            .Where(c => c.DeletedAt == null && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => c.Name)
            .ToListAsync(ct);

        if (categories.Count > 0)
        {
            sb.AppendLine("DANH MỤC HÀNG HÓA TẠI CỬA HÀNG:");
            sb.AppendLine(string.Join(", ", categories));
            sb.AppendLine();
        }

        // 2. Active Coupons
        DateTimeOffset now = timeProvider.GetUtcNow();
        var coupons = await dbContext.Coupons
            .AsNoTracking()
            .Where(c => c.IsActive && c.StartsAt <= now && c.EndsAt >= now)
            .Select(c => new { c.Code, c.Type, c.Value, c.MinSubtotal, c.Description })
            .Take(5)
            .ToListAsync(ct);

        if (coupons.Count > 0)
        {
            sb.AppendLine("MÃ KHUYẾN MÃI / GIẢM GIÁ HIỆN CÓ:");
            foreach (var cp in coupons)
            {
                string val = string.Equals(cp.Type, "Percent", StringComparison.OrdinalIgnoreCase)
                    ? $"{cp.Value}%"
                    : $"{cp.Value:N0} ₫";
                sb.AppendLine($"- Mã `{cp.Code}`: Giảm {val} cho đơn từ {cp.MinSubtotal:N0} ₫ ({cp.Description})");
            }
            sb.AppendLine();
        }

        // 3. Products
        var allProducts = await dbContext.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.DeletedAt == null && p.Status == ProductStatus.Active)
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.SoldCount)
            .Select(p => new
            {
                p.Name,
                p.Slug,
                p.SellingPrice,
                p.Unit,
                p.PackSize,
                CategoryName = p.Category != null ? p.Category.Name : "Khác",
                p.ShortDescription,
                p.IsFeatured
            })
            .ToListAsync(ct);

        if (allProducts.Count > 0)
        {
            string lowerMsg = userMessage.ToLowerInvariant();
            HashSet<string> ignoreWords = new(StringComparer.OrdinalIgnoreCase)
            {
                "cho", "tôi", "mình", "bạn", "shop", "có", "không", "giá", "nào", "bao", "nhiêu", "nhé", "ạ", "ơi",
                "hỏi", "về", "những", "các", "loại", "mua", "cần", "muốn", "tìm", "kiếm", "xem", "được", "lấy", "ra", "để", "thế"
            };

            List<string> keywords = lowerMsg
                .Split([' ', ',', '.', '?', '!', ';', ':', '/', '\\', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length >= 2 && !ignoreWords.Contains(w))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .ToList();

            var matchedProducts = allProducts.Where(p =>
                keywords.Any(k => p.Name.Contains(k, StringComparison.OrdinalIgnoreCase)
                               || p.CategoryName.Contains(k, StringComparison.OrdinalIgnoreCase)
                               || (!string.IsNullOrEmpty(p.ShortDescription) && p.ShortDescription.Contains(k, StringComparison.OrdinalIgnoreCase))))
                .Take(25)
                .ToList();

            if (matchedProducts.Count > 0)
            {
                sb.AppendLine("SẢN PHẨM KHỚP NHẤT VỚI YÊU CẦU CỦA KHÁCH:");
                foreach (var p in matchedProducts)
                {
                    sb.AppendLine($"- **{p.Name}** ({p.CategoryName}): {p.SellingPrice:N0} ₫ / {p.Unit} — Link: [Xem chi tiết](/product/{p.Slug})");
                    if (!string.IsNullOrWhiteSpace(p.ShortDescription))
                    {
                        sb.AppendLine($"  *Mô tả:* {p.ShortDescription}");
                    }
                }
                sb.AppendLine();
            }

            // Category summary with sample products
            sb.AppendLine("DANH SÁCH SẢN PHẨM TIÊU BIỂU THEO DANH MỤC:");
            var groups = allProducts.GroupBy(p => p.CategoryName);
            foreach (var grp in groups)
            {
                var itemsStr = string.Join("; ", grp.Take(15).Select(p => $"{p.Name} ({p.SellingPrice:N0} ₫/{p.Unit}, link: [Xem](/product/{p.Slug}))"));
                sb.AppendLine($"• **{grp.Key}**: {itemsStr}");
            }
        }

        return sb.ToString();
    }
}
