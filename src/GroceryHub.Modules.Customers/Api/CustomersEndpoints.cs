using System.Security.Claims;
using FluentValidation;
using GroceryHub.Modules.Customers.Application.DTOs;
using GroceryHub.Modules.Customers.Application.Services;
using GroceryHub.SharedKernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GroceryHub.Modules.Customers.Api;

internal static class CustomersEndpoints
{
    public static RouteGroupBuilder MapCustomersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        MapLocationsEndpoints(endpoints);
        MapMeEndpoints(endpoints);
        MapAdminEndpoints(endpoints);

        return (RouteGroupBuilder)endpoints;
    }

    private static void MapLocationsEndpoints(IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/locations")
            .WithTags("Locations");

        group.MapGet("/provinces", async (
            CustomersService customersService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Result<ProvincesResponse> result = await customersService.GetProvincesAsync(ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : CustomersResults.Problem(context, result.Error);
        })
        .WithName("GetProvinces")
        .WithSummary("Get list of all 34 provinces and their serviceability status")
        .Produces<ProvincesResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .AllowAnonymous()
        .RequireRateLimiting("public-read");

        group.MapGet("/provinces/{code}/wards", async (
            string code,
            CustomersService customersService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Result<WardsResponse> result = await customersService.GetWardsAsync(code, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : CustomersResults.Problem(context, result.Error);
        })
        .WithName("GetWards")
        .WithSummary("Get list of wards for a given province code")
        .Produces<WardsResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .AllowAnonymous()
        .RequireRateLimiting("public-read");
    }

    private static void MapMeEndpoints(IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/me")
            .WithTags("Customer Profile & Addresses")
            .RequireAuthorization("auth");

        group.MapGet("/profile", async (
            ClaimsPrincipal user,
            CustomersService customersService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid? userId = GetUserIdFromClaims(user);
            if (!userId.HasValue)
            {
                return CustomersResults.Problem(context, DomainError.Unauthorized());
            }

            Result<CustomerProfileDto> result = await customersService.GetProfileAsync(userId.Value, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : CustomersResults.Problem(context, result.Error);
        })
        .WithName("GetCustomerProfile")
        .WithSummary("Get current customer profile")
        .Produces<CustomerProfileDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/profile", async (
            [FromBody] UpdateProfileRequest request,
            IValidator<UpdateProfileRequest> validator,
            ClaimsPrincipal user,
            CustomersService customersService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid? userId = GetUserIdFromClaims(user);
            if (!userId.HasValue)
            {
                return CustomersResults.Problem(context, DomainError.Unauthorized());
            }

            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return CustomersResults.Problem(context, DomainError.ValidationFailed(
                    ToFieldErrors(validation),
                    "Profile validation failed."));
            }

            Result<UpdateProfileResponse> result = await customersService.UpdateProfileAsync(userId.Value, request, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : CustomersResults.Problem(context, result.Error);
        })
        .WithName("UpdateCustomerProfile")
        .WithSummary("Update current customer profile")
        .Produces<UpdateProfileResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/addresses", async (
            ClaimsPrincipal user,
            CustomersService customersService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid? userId = GetUserIdFromClaims(user);
            if (!userId.HasValue)
            {
                return CustomersResults.Problem(context, DomainError.Unauthorized());
            }

            Result<IReadOnlyList<AddressDto>> result = await customersService.GetAddressesAsync(userId.Value, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : CustomersResults.Problem(context, result.Error);
        })
        .WithName("GetCustomerAddresses")
        .WithSummary("Get list of active addresses for current customer")
        .Produces<IReadOnlyList<AddressDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/addresses", async (
            [FromBody] CreateAddressRequest request,
            IValidator<CreateAddressRequest> validator,
            ClaimsPrincipal user,
            CustomersService customersService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid? userId = GetUserIdFromClaims(user);
            if (!userId.HasValue)
            {
                return CustomersResults.Problem(context, DomainError.Unauthorized());
            }

            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return CustomersResults.Problem(context, DomainError.ValidationFailed(
                    ToFieldErrors(validation),
                    "Address validation failed."));
            }

            Result<AddressDto> result = await customersService.CreateAddressAsync(userId.Value, request, ct);
            return result.IsSuccess
                ? TypedResults.Created($"/api/v1/me/addresses/{result.Value.Id}", result.Value)
                : CustomersResults.Problem(context, result.Error);
        })
        .WithName("CreateCustomerAddress")
        .WithSummary("Create a new delivery address (max 10 active addresses)")
        .Produces<AddressDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPut("/addresses/{id:guid}", async (
            Guid id,
            [FromBody] UpdateAddressRequest request,
            IValidator<UpdateAddressRequest> validator,
            ClaimsPrincipal user,
            CustomersService customersService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid? userId = GetUserIdFromClaims(user);
            if (!userId.HasValue)
            {
                return CustomersResults.Problem(context, DomainError.Unauthorized());
            }

            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return CustomersResults.Problem(context, DomainError.ValidationFailed(
                    ToFieldErrors(validation),
                    "Address validation failed."));
            }

            Result<AddressDto> result = await customersService.UpdateAddressAsync(userId.Value, id, request, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : CustomersResults.Problem(context, result.Error);
        })
        .WithName("UpdateCustomerAddress")
        .WithSummary("Update an existing delivery address")
        .Produces<AddressDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapDelete("/addresses/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            CustomersService customersService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid? userId = GetUserIdFromClaims(user);
            if (!userId.HasValue)
            {
                return CustomersResults.Problem(context, DomainError.Unauthorized());
            }

            Result result = await customersService.DeleteAddressAsync(userId.Value, id, ct);
            return result.IsSuccess
                ? TypedResults.NoContent()
                : CustomersResults.Problem(context, result.Error);
        })
        .WithName("DeleteCustomerAddress")
        .WithSummary("Soft delete an address")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/addresses/{id:guid}/default", async (
            Guid id,
            ClaimsPrincipal user,
            CustomersService customersService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid? userId = GetUserIdFromClaims(user);
            if (!userId.HasValue)
            {
                return CustomersResults.Problem(context, DomainError.Unauthorized());
            }

            Result<AddressDto> result = await customersService.SetDefaultAddressAsync(userId.Value, id, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : CustomersResults.Problem(context, result.Error);
        })
        .WithName("SetDefaultCustomerAddress")
        .WithSummary("Set default delivery address")
        .Produces<AddressDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapAdminEndpoints(IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/admin/customers")
            .WithTags("Admin Customers");

        group.MapGet("/", async (
            [FromQuery] string? q,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 24,
            CustomersService customersService = default!,
            HttpContext context = default!,
            CancellationToken ct = default) =>
        {
            Result<PagedResponse<AdminCustomerListItemDto>> result = await customersService.GetAdminCustomersAsync(q, status, page, pageSize, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : CustomersResults.Problem(context, result.Error);
        })
        .WithName("GetAdminCustomers")
        .WithSummary("List customers with pagination and filtering")
        .Produces<PagedResponse<AdminCustomerListItemDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization("customers.read");

        group.MapGet("/{id:guid}", async (
            Guid id,
            CustomersService customersService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Result<AdminCustomerDetailDto> result = await customersService.GetAdminCustomerByIdAsync(id, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : CustomersResults.Problem(context, result.Error);
        })
        .WithName("GetAdminCustomerById")
        .WithSummary("Get customer detail by id")
        .Produces<AdminCustomerDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization("customers.read");

        group.MapPost("/{id:guid}/lock", async (
            Guid id,
            [FromBody] LockCustomerRequest request,
            ClaimsPrincipal user,
            CustomersService customersService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid actorId = GetUserIdFromClaims(user) ?? Guid.Empty;
            Result result = await customersService.LockCustomerAsync(id, request.Reason, actorId, ct);
            return result.IsSuccess
                ? TypedResults.Ok(new { id, status = "Locked" })
                : CustomersResults.Problem(context, result.Error);
        })
        .WithName("LockCustomer")
        .WithSummary("Lock a customer account")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization("customers.lock");

        group.MapPost("/{id:guid}/unlock", async (
            Guid id,
            [FromBody] LockCustomerRequest request,
            ClaimsPrincipal user,
            CustomersService customersService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid actorId = GetUserIdFromClaims(user) ?? Guid.Empty;
            Result result = await customersService.UnlockCustomerAsync(id, request.Reason, actorId, ct);
            return result.IsSuccess
                ? TypedResults.Ok(new { id, status = "Active" })
                : CustomersResults.Problem(context, result.Error);
        })
        .WithName("UnlockCustomer")
        .WithSummary("Unlock a customer account")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization("customers.lock");

        group.MapPost("/{id:guid}/anonymize", async (
            Guid id,
            [FromBody] AnonymizeCustomerRequest request,
            ClaimsPrincipal user,
            CustomersService customersService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid actorId = GetUserIdFromClaims(user) ?? Guid.Empty;
            Result<DateTimeOffset> result = await customersService.AnonymizeCustomerAsync(id, request.Reason, actorId, ct);
            return result.IsSuccess
                ? TypedResults.Ok(new { id, anonymizedAt = result.Value })
                : CustomersResults.Problem(context, result.Error);
        })
        .WithName("AnonymizeCustomer")
        .WithSummary("Scrub customer PII for GDPR/privacy compliance")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireAuthorization("staff.manage");
    }

    private static Guid? GetUserIdFromClaims(ClaimsPrincipal user)
    {
        string? sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        return Guid.TryParse(sub, out Guid parsed) ? parsed : null;
    }

    private static Dictionary<string, string[]> ToFieldErrors(FluentValidation.Results.ValidationResult validation)
    {
        return validation.Errors
            .GroupBy(static e => e.PropertyName)
            .ToDictionary(
                static g => g.Key,
                static g => g.Select(static e => e.ErrorMessage).ToArray());
    }
}
