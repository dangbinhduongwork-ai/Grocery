using GroceryHub.SharedKernel;

namespace GroceryHub.Persistence.Entities;

public static class BannerPlacement
{
    public const string HomeHero = "HomeHero";
    public const string HomeMid = "HomeMid";
    public const string AisleTop = "AisleTop";

    public static readonly IReadOnlyList<string> All = [HomeHero, HomeMid, AisleTop];
}

public class Banner : IAuditableEntity
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    public string ImageKey { get; set; } = string.Empty;

    public string? LinkUrl { get; set; }

    public string Placement { get; set; } = BannerPlacement.HomeHero;

    public int SortOrder { get; set; }

    public DateTimeOffset? StartsAt { get; set; }

    public DateTimeOffset? EndsAt { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
