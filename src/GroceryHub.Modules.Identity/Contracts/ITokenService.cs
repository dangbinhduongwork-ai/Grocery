using GroceryHub.SharedKernel;

namespace GroceryHub.Modules.Identity.Contracts;

public sealed record GeneratedTokens(
    string AccessToken,
    int ExpiresIn,
    string RawRefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    Guid FamilyId);

public sealed record RefreshResult(
    GeneratedTokens Tokens,
    Guid UserId);

public interface ITokenService
{
    string GenerateAccessToken(
        Guid userId,
        string email,
        string fullName,
        IReadOnlyList<string> roles);

    Task<GeneratedTokens> GenerateTokensAsync(
        Guid userId,
        string email,
        string fullName,
        IReadOnlyList<string> roles,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default);

    Task<Result<RefreshResult>> RotateRefreshTokenAsync(
        string rawRefreshToken,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default);

    Task<Result> RevokeFamilyAsync(
        string rawRefreshToken,
        CancellationToken ct = default);

    Task RevokeAllUserTokensAsync(
        Guid userId,
        CancellationToken ct = default);

    Task RevokeOtherFamiliesAsync(
        Guid userId,
        Guid keepFamilyId,
        CancellationToken ct = default);
}
