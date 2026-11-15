using System.ComponentModel.DataAnnotations;

namespace GroceryHub.Api.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required(AllowEmptyStrings = false)]
    public string DefaultConnection { get; init; } = string.Empty;
}

public sealed class SystemOptions
{
    public const string SectionName = "GroceryHub";

    [Required(AllowEmptyStrings = false)]
    public string ServiceName { get; init; } = "GroceryHub.Api";

    [Required(AllowEmptyStrings = false)]
    public string EnvironmentName { get; init; } = "Development";

    [Required(AllowEmptyStrings = false)]
    public string Version { get; init; } = "1.0.0";
}

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public const string AuthPolicyName = "auth";
    public const string CheckoutPolicyName = "checkout";
    public const string PublicReadPolicyName = "public-read";

    [Range(1, 10000)]
    public int AuthPermitLimitPerMinute { get; init; } = 5;

    [Range(1, 10000)]
    public int CheckoutPermitLimitPerMinute { get; init; } = 10;

    [Range(1, 100000)]
    public int PublicReadPermitLimitPerMinute { get; init; } = 120;
}
