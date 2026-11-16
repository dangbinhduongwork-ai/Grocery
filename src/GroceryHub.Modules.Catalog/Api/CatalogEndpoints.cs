using System.Security.Claims;
using FluentValidation;
using GroceryHub.Modules.Catalog.Application.DTOs;
using GroceryHub.Modules.Catalog.Application.Services;
using GroceryHub.Modules.Catalog.Application.Storage;
using GroceryHub.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GroceryHub.Modules.Catalog.Api;

internal static class CatalogEndpoints
{
    private const string CatalogRead = "catalog.read";
    private const string CatalogWrite = "catalog.write";
    private const string CatalogPriceWrite = "catalog.price.write";
    private const string CatalogCostRead = "catalog.cost.read";
    public static RouteGroupBuilder MapCatalogStorefrontEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("")
            .WithTags("Storefront Catalog");

        group.MapGet("/categories", async (CatalogQueryService queryService, CancellationToken ct) =>
        {
            var categories = await queryService.GetCategoryTreeAsync(ct);
            return TypedResults.Ok(categories);
        })
        .WithName("GetStorefrontCategories")
        .WithSummary("Get hierarchical category tree with product counts")
        .Produces<IReadOnlyList<CategoryTreeDto>>(StatusCodes.Status200OK)
        .AllowAnonymous();

        group.MapGet("/brands", async (CatalogQueryService queryService, CancellationToken ct) =>
        {
            var brands = await queryService.GetBrandsAsync(ct);
            return TypedResults.Ok(brands);
        })
        .WithName("GetStorefrontBrands")
        .WithSummary("Get active brands list")
        .Produces<IReadOnlyList<BrandDto>>(StatusCodes.Status200OK)
        .AllowAnonymous();

        group.MapGet("/products", async (
            [FromQuery] string? q,
            [FromQuery] string? search,
            [FromQuery] string? categorySlug,
            [FromQuery] Guid? categoryId,
            [FromQuery] string? brandSlug,
            [FromQuery] Guid? brandId,
            [FromQuery] long? minPrice,
            [FromQuery] long? maxPrice,
            [FromQuery] bool? inStockOnly,
            [FromQuery] bool? inStock,
            [FromQuery] string? sort,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CatalogQueryService queryService,
            CancellationToken ct) =>
        {
            string? searchKeyword = !string.IsNullOrWhiteSpace(q) ? q : search;
            bool filterInStock = (inStockOnly ?? false) || (inStock ?? false);
            var result = await queryService.SearchProductsAsync(
                searchKeyword,
                categorySlug,
                brandSlug,
                minPrice,
                maxPrice,
                filterInStock,
                sort,
                page ?? 1,
                pageSize ?? 24,
                ct,
                categoryId,
                brandId);

            return TypedResults.Ok(result.Value);
        })
        .WithName("SearchStorefrontProducts")
        .WithSummary("Search, filter, and paginate storefront products")
        .Produces<PagedResponse<StorefrontProductCardDto>>(StatusCodes.Status200OK)
        .AllowAnonymous();

        group.MapGet("/products/{slug}", async (
            string slug,
            CatalogQueryService queryService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var result = await queryService.GetProductBySlugAsync(slug, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : ToProblem(context, result.Error);
        })
        .WithName("GetStorefrontProductBySlug")
        .WithSummary("Get storefront product detail by URL slug")
        .Produces<StorefrontProductDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AllowAnonymous();

        group.MapGet("/products/{id:guid}/related", async (
            Guid id,
            CatalogQueryService queryService,
            CancellationToken ct) =>
        {
            var related = await queryService.GetRelatedProductsAsync(id, ct);
            return TypedResults.Ok(related);
        })
        .WithName("GetRelatedStorefrontProducts")
        .WithSummary("Get related products in the same category")
        .Produces<IReadOnlyList<StorefrontProductCardDto>>(StatusCodes.Status200OK)
        .AllowAnonymous();

        group.MapGet("/banners", async (
            [FromQuery] string? placement,
            CatalogQueryService queryService,
            CancellationToken ct) =>
        {
            var banners = await queryService.GetBannersAsync(placement, ct);
            return TypedResults.Ok(banners);
        })
        .WithName("GetStorefrontBanners")
        .WithSummary("Get active promotional banners")
        .Produces<IReadOnlyList<BannerDto>>(StatusCodes.Status200OK)
        .AllowAnonymous();

        group.MapGet("/home", async (
            CatalogQueryService queryService,
            CancellationToken ct) =>
        {
            var home = await queryService.GetHomeStorefrontAsync(ct);
            return TypedResults.Ok(home);
        })
        .WithName("GetStorefrontHome")
        .WithSummary("Get storefront home aggregate data")
        .Produces<HomeStorefrontDto>(StatusCodes.Status200OK)
        .AllowAnonymous();

        group.MapGet("/settings/ui", async (
            CatalogAdminService adminService,
            CancellationToken ct) =>
        {
            var settings = await adminService.GetUiDesignSettingsAsync(ct);
            return TypedResults.Ok(settings);
        })
        .WithName("GetStorefrontUiSettings")
        .WithSummary("Get active UI design settings, marquee announcements and slogans")
        .Produces<UiDesignSettingsDto>(StatusCodes.Status200OK)
        .AllowAnonymous();

        group.MapGet("/catalog/images/{key}", async (
            string key,
            IFileStorage fileStorage,
            CancellationToken ct) =>
        {
            Stream? stream = await fileStorage.OpenReadAsync(key, ct);
            if (stream == null)
            {
                return Results.NotFound();
            }

            string ext = Path.GetExtension(key).ToLowerInvariant();
            string mime = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            return Results.File(stream, mime, enableRangeProcessing: true);
        })
        .WithName("GetCatalogImage")
        .WithSummary("Stream stored product image file")
        .Produces(StatusCodes.Status200OK, contentType: "image/jpeg")
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AllowAnonymous();

        return group;
    }

    public static RouteGroupBuilder MapCatalogAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/admin")
            .WithTags("Catalog Admin");

        // GET /admin/products
        group.MapGet("/products", async (
            [FromQuery] string? q,
            [FromQuery] Guid? categoryId,
            [FromQuery] Guid? brandId,
            [FromQuery] string? status,
            [FromQuery] bool? lowStock,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ClaimsPrincipal user,
            IAuthorizationService authorizationService,
            CatalogAdminService adminService,
            CancellationToken ct) =>
        {
            bool canReadCost = await CheckCostPermissionAsync(user, authorizationService);
            var result = await adminService.GetAdminProductsAsync(
                q,
                categoryId,
                brandId,
                status,
                lowStock,
                page ?? 1,
                pageSize ?? 24,
                canReadCost,
                ct);

            return TypedResults.Ok(result.Value);
        })
        .WithName("GetAdminProducts")
        .WithSummary("Get admin products list with pagination and filtering")
        .Produces<PagedResponse<AdminProductListItemDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(CatalogRead);

        // POST /admin/products
        group.MapPost("/products", async (
            [FromBody] CreateProductRequest request,
            IValidator<CreateProductRequest> validator,
            CatalogAdminService adminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return ToValidationProblem(context, validation);
            }

            var result = await adminService.CreateProductAsync(request, ct);
            return result.IsSuccess
                ? TypedResults.Created($"/api/v1/admin/products/{result.Value.Id}", result.Value)
                : ToProblem(context, result.Error);
        })
        .WithName("CreateAdminProduct")
        .WithSummary("Create a new product and initialize inventory stock")
        .Produces<AdminProductDetailDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(CatalogWrite);

        // GET /admin/products/{id}
        group.MapGet("/products/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            IAuthorizationService authorizationService,
            CatalogAdminService adminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            bool canReadCost = await CheckCostPermissionAsync(user, authorizationService);
            var result = await adminService.GetAdminProductByIdAsync(id, canReadCost, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : ToProblem(context, result.Error);
        })
        .WithName("GetAdminProductById")
        .WithSummary("Get product details by ID for administration")
        .Produces<AdminProductDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(CatalogRead);

        // PUT /admin/products/{id}
        group.MapPut("/products/{id:guid}", async (
            Guid id,
            [FromBody] UpdateProductRequest request,
            [FromHeader(Name = "If-Match")] string? ifMatch,
            IValidator<UpdateProductRequest> validator,
            CatalogAdminService adminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return ToValidationProblem(context, validation);
            }

            var result = await adminService.UpdateProductAsync(id, request, ifMatch, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : ToProblem(context, result.Error);
        })
        .WithName("UpdateAdminProduct")
        .WithSummary("Update product information")
        .Produces<AdminProductDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status412PreconditionFailed)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(CatalogWrite);

        // PUT /admin/products/{id}/status
        group.MapPut("/products/{id:guid}/status", async (
            Guid id,
            [FromBody] UpdateProductStatusRequest request,
            CatalogAdminService adminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var result = await adminService.UpdateProductStatusAsync(id, request.Status, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : ToProblem(context, result.Error);
        })
        .WithName("UpdateAdminProductStatus")
        .WithSummary("Update product status (Draft, Active, Hidden)")
        .Produces<ProductStatusResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(CatalogWrite);

        // DELETE /admin/products/{id}
        group.MapDelete("/products/{id:guid}", async (
            Guid id,
            CatalogAdminService adminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var result = await adminService.DeleteProductAsync(id, ct);
            return result.IsSuccess
                ? TypedResults.NoContent()
                : ToProblem(context, result.Error);
        })
        .WithName("DeleteAdminProduct")
        .WithSummary("Soft delete a product from the catalog")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(CatalogWrite);

        // PUT /admin/products/{id}/price
        group.MapPut("/products/{id:guid}/price", async (
            Guid id,
            [FromBody] UpdateProductPriceRequest request,
            [FromHeader(Name = "If-Match")] string? ifMatch,
            IValidator<UpdateProductPriceRequest> validator,
            ClaimsPrincipal user,
            CatalogAdminService adminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return ToValidationProblem(context, validation);
            }

            Guid? actorUserId = GetUserId(user);
            string? actorRole = user.FindFirstValue(ClaimTypes.Role);
            string? ip = context.Connection.RemoteIpAddress?.ToString();
            string? traceId = context.TraceIdentifier;

            var result = await adminService.UpdateProductPriceAsync(
                id,
                request,
                actorUserId,
                actorRole,
                ifMatch,
                ip,
                traceId,
                ct);

            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : ToProblem(context, result.Error);
        })
        .WithName("UpdateAdminProductPrice")
        .WithSummary("Update product selling and list price with audit trail")
        .Produces<ProductPriceResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status412PreconditionFailed)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(CatalogPriceWrite);

        // POST /admin/products/{id}/images
        group.MapPost("/products/{id:guid}/images", async (
            Guid id,
            IFormFile file,
            [FromForm] string? alt,
            [FromForm] bool? isPrimary,
            CatalogAdminService adminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            if (file == null || file.Length == 0)
            {
                return ToProblem(context, DomainError.Validation("FILE_EMPTY", "File cannot be empty."));
            }

            await using Stream stream = file.OpenReadStream();
            var result = await adminService.AddProductImageAsync(
                id,
                stream,
                file.ContentType,
                file.Length,
                alt,
                isPrimary ?? false,
                ct);

            return result.IsSuccess
                ? TypedResults.Created($"/api/v1/admin/products/{id}/images/{result.Value.Id}", result.Value)
                : ToProblem(context, result.Error);
        })
        .WithName("UploadProductImage")
        .WithSummary("Upload and sanitize product image (JPEG, PNG, WebP <= 5MB)")
        .Produces<ProductImageDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .DisableAntiforgery()
        .RequireAuthorization(CatalogWrite);

        // PUT /admin/products/{id}/images/order AND /reorder
        async Task<IResult> ReorderImagesHandler(
            Guid id,
            ReorderProductImagesRequest request,
            CatalogAdminService adminService,
            HttpContext context,
            CancellationToken ct)
        {
            var result = await adminService.ReorderProductImagesAsync(id, request, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : ToProblem(context, result.Error);
        }

        group.MapPut("/products/{id:guid}/images/order", ReorderImagesHandler)
            .WithName("ReorderProductImagesOrder")
            .WithSummary("Reorder product images and set primary image")
            .Produces<IReadOnlyList<ProductImageDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .RequireAuthorization(CatalogWrite);

        group.MapPut("/products/{id:guid}/images/reorder", ReorderImagesHandler)
            .WithName("ReorderProductImagesReorder")
            .WithSummary("Reorder product images and set primary image (alias)")
            .Produces<IReadOnlyList<ProductImageDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .RequireAuthorization(CatalogWrite);

        // DELETE /admin/products/{id}/images/{imageId}
        group.MapDelete("/products/{id:guid}/images/{imageId:guid}", async (
            Guid id,
            Guid imageId,
            CatalogAdminService adminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var result = await adminService.DeleteProductImageAsync(id, imageId, ct);
            return result.IsSuccess
                ? TypedResults.NoContent()
                : ToProblem(context, result.Error);
        })
        .WithName("DeleteProductImage")
        .WithSummary("Delete product image")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(CatalogWrite);

        // Categories Admin
        group.MapGet("/categories", async (
            CatalogAdminService adminService,
            CancellationToken ct) =>
        {
            var categories = await adminService.GetAdminCategoriesAsync(ct);
            return TypedResults.Ok(categories);
        })
        .WithName("GetAdminCategories")
        .WithSummary("Get admin categories list")
        .Produces<IReadOnlyList<AdminCategoryDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(CatalogRead);

        group.MapPost("/categories", async (
            [FromBody] CreateCategoryRequest request,
            IValidator<CreateCategoryRequest> validator,
            CatalogAdminService adminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return ToValidationProblem(context, validation);
            }

            var result = await adminService.CreateCategoryAsync(request, ct);
            return result.IsSuccess
                ? TypedResults.Created($"/api/v1/admin/categories/{result.Value.Id}", result.Value)
                : ToProblem(context, result.Error);
        })
        .WithName("CreateAdminCategory")
        .WithSummary("Create a new category")
        .Produces<AdminCategoryDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(CatalogWrite);

        group.MapPut("/categories/{id:guid}", async (
            Guid id,
            [FromBody] UpdateCategoryRequest request,
            IValidator<UpdateCategoryRequest> validator,
            CatalogAdminService adminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return ToValidationProblem(context, validation);
            }

            var result = await adminService.UpdateCategoryAsync(id, request, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : ToProblem(context, result.Error);
        })
        .WithName("UpdateAdminCategory")
        .WithSummary("Update category details")
        .Produces<AdminCategoryDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(CatalogWrite);

        group.MapDelete("/categories/{id:guid}", async (
            Guid id,
            CatalogAdminService adminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var result = await adminService.DeleteCategoryAsync(id, ct);
            return result.IsSuccess
                ? TypedResults.NoContent()
                : ToProblem(context, result.Error);
        })
        .WithName("DeleteAdminCategory")
        .WithSummary("Delete category (blocked if category contains subcategories or products)")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(CatalogWrite);

        // Brands Admin
        group.MapGet("/brands", async (
            CatalogAdminService adminService,
            CancellationToken ct) =>
        {
            var brands = await adminService.GetAdminBrandsAsync(ct);
            return TypedResults.Ok(brands);
        })
        .WithName("GetAdminBrands")
        .WithSummary("Get admin brands list")
        .Produces<IReadOnlyList<AdminBrandDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(CatalogRead);

        group.MapPost("/brands", async (
            [FromBody] CreateBrandRequest request,
            IValidator<CreateBrandRequest> validator,
            CatalogAdminService adminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return ToValidationProblem(context, validation);
            }

            var result = await adminService.CreateBrandAsync(request, ct);
            return result.IsSuccess
                ? TypedResults.Created($"/api/v1/admin/brands/{result.Value.Id}", result.Value)
                : ToProblem(context, result.Error);
        })
        .WithName("CreateAdminBrand")
        .WithSummary("Create a new brand")
        .Produces<AdminBrandDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(CatalogWrite);

        group.MapPut("/brands/{id:guid}", async (
            Guid id,
            [FromBody] UpdateBrandRequest request,
            IValidator<UpdateBrandRequest> validator,
            CatalogAdminService adminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return ToValidationProblem(context, validation);
            }

            var result = await adminService.UpdateBrandAsync(id, request, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : ToProblem(context, result.Error);
        })
        .WithName("UpdateAdminBrand")
        .WithSummary("Update brand details")
        .Produces<AdminBrandDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(CatalogWrite);

        group.MapDelete("/brands/{id:guid}", async (
            Guid id,
            CatalogAdminService adminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var result = await adminService.DeleteBrandAsync(id, ct);
            return result.IsSuccess
                ? TypedResults.NoContent()
                : ToProblem(context, result.Error);
        })
        .WithName("DeleteAdminBrand")
        .WithSummary("Delete brand (blocked if products reference it)")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(CatalogWrite);

        // GET /admin/banners
        group.MapGet("/banners", async (
            CatalogAdminService adminService,
            CancellationToken ct) =>
        {
            var banners = await adminService.GetAdminBannersAsync(ct);
            return TypedResults.Ok(banners);
        })
        .WithName("GetAdminBanners")
        .WithSummary("Get all promotional banners for administration")
        .Produces<IReadOnlyList<AdminBannerDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(CatalogRead);

        // GET /admin/banners/{id}
        group.MapGet("/banners/{id:guid}", async (
            Guid id,
            CatalogAdminService adminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var result = await adminService.GetAdminBannerByIdAsync(id, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : ToProblem(context, result.Error);
        })
        .WithName("GetAdminBannerById")
        .WithSummary("Get banner by ID")
        .Produces<AdminBannerDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(CatalogRead);

        // POST /admin/banners
        group.MapPost("/banners", async (
            [FromBody] CreateBannerRequest request,
            IValidator<CreateBannerRequest> validator,
            CatalogAdminService adminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return ToValidationProblem(context, validation);
            }

            var result = await adminService.CreateBannerAsync(request, ct);
            return result.IsSuccess
                ? TypedResults.Created($"/api/v1/admin/banners/{result.Value.Id}", result.Value)
                : ToProblem(context, result.Error);
        })
        .WithName("CreateAdminBanner")
        .WithSummary("Create promotional banner")
        .Produces<AdminBannerDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(CatalogWrite);

        // PUT /admin/banners/{id}
        group.MapPut("/banners/{id:guid}", async (
            Guid id,
            [FromBody] UpdateBannerRequest request,
            IValidator<UpdateBannerRequest> validator,
            CatalogAdminService adminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return ToValidationProblem(context, validation);
            }

            var result = await adminService.UpdateBannerAsync(id, request, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : ToProblem(context, result.Error);
        })
        .WithName("UpdateAdminBanner")
        .WithSummary("Update banner")
        .Produces<AdminBannerDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(CatalogWrite);

        // DELETE /admin/banners/{id}
        group.MapDelete("/banners/{id:guid}", async (
            Guid id,
            CatalogAdminService adminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var result = await adminService.DeleteBannerAsync(id, ct);
            return result.IsSuccess
                ? TypedResults.NoContent()
                : ToProblem(context, result.Error);
        })
        .WithName("DeleteAdminBanner")
        .WithSummary("Delete banner")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(CatalogWrite);

        // POST /admin/banners/upload-image
        group.MapPost("/banners/upload-image", async (
            IFormFile file,
            CatalogAdminService adminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            if (file == null || file.Length == 0)
            {
                return ToProblem(context, DomainError.Validation("Image.Required", "Vui lòng chọn tệp hình ảnh để tải lên."));
            }

            await using var stream = file.OpenReadStream();
            var result = await adminService.UploadBannerImageAsync(stream, file.ContentType, file.Length, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : ToProblem(context, result.Error);
        })
        .WithName("UploadAdminBannerImage")
        .WithSummary("Upload and sanitize banner image")
        .Produces<BannerImageUploadResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .DisableAntiforgery()
        .RequireAuthorization(CatalogWrite);

        // GET /admin/settings/ui
        group.MapGet("/settings/ui", async (
            CatalogAdminService adminService,
            CancellationToken ct) =>
        {
            var settings = await adminService.GetUiDesignSettingsAsync(ct);
            return TypedResults.Ok(settings);
        })
        .WithName("GetAdminUiSettings")
        .WithSummary("Get UI design settings for administration")
        .Produces<UiDesignSettingsDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(CatalogRead);

        // PUT /admin/settings/ui
        group.MapPut("/settings/ui", async (
            [FromBody] UiDesignSettingsDto request,
            ClaimsPrincipal user,
            CatalogAdminService adminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            var result = await adminService.UpdateUiDesignSettingsAsync(request, userId, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : ToProblem(context, result.Error);
        })
        .WithName("UpdateAdminUiSettings")
        .WithSummary("Update UI design settings (announcements, slogans)")
        .Produces<UiDesignSettingsDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(CatalogWrite);

        return group;
    }

    private static async Task<bool> CheckCostPermissionAsync(
        ClaimsPrincipal user,
        IAuthorizationService authorizationService)
    {
        var authResult = await authorizationService.AuthorizeAsync(user, CatalogCostRead);
        return authResult.Succeeded;
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        string? val = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(val, out Guid id) ? id : null;
    }

    private static IResult ToProblem(HttpContext context, DomainError error)
    {
        return CatalogResults.Problem(context, error);
    }

    private static IResult ToValidationProblem(HttpContext context, FluentValidation.Results.ValidationResult validation)
    {
        var dict = validation.Errors
            .GroupBy(static e => e.PropertyName)
            .ToDictionary(
                static g => g.Key,
                static g => g.Select(static e => e.ErrorMessage).ToArray());

        DomainError domainError = DomainError.Validation(ErrorCodes.ValidationFailed, "Validation failed.", dict);
        return CatalogResults.Problem(context, domainError);
    }
}
