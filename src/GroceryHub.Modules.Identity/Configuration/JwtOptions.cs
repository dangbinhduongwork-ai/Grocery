using System.ComponentModel.DataAnnotations;

namespace GroceryHub.Modules.Identity.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    [MinLength(32, ErrorMessage = "JWT signing key must be at least 256 bits (32 characters).")]
    public string SecretKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "GroceryHub";

    public string Audience { get; set; } = "GroceryHubClient";

    public int AccessTokenExpirationMinutes { get; set; } = 15;

    public int RefreshTokenExpirationDays { get; set; } = 14;
}
