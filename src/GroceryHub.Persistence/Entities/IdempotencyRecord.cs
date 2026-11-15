namespace GroceryHub.Persistence.Entities;

public sealed class IdempotencyRecord
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    public string RequestHash { get; set; } = string.Empty;

    public int ResponseStatus { get; set; }

    public string ResponseContentType { get; set; } = "application/json";

    public string ResponseBody { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
}
