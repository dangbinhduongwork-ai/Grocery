using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GroceryHub.Modules.Identity.Configuration;
using GroceryHub.Modules.Identity.Contracts;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GroceryHub.Modules.Identity.Application;

public sealed class TokenService : ITokenService
{
    private readonly GroceryHubDbContext _dbContext;
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;

    public TokenService(
        GroceryHubDbContext dbContext,
        IOptions<JwtOptions> options,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public string GenerateAccessToken(
        Guid userId,
        string email,
        string fullName,
        IReadOnlyList<string> roles)
    {
        ArgumentNullException.ThrowIfNull(roles);

        DateTimeOffset now = _timeProvider.GetUtcNow();
        DateTimeOffset expiresAt = now.AddMinutes(_options.AccessTokenExpirationMinutes);

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.Email, email),
            new(JwtRegisteredClaimNames.Name, fullName),
            new(ClaimTypes.Name, fullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        ];

        foreach (string role in roles)
        {
            if (!string.IsNullOrWhiteSpace(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
                claims.Add(new Claim("role", role));
            }
        }

        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_options.SecretKey));
        SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<GeneratedTokens> GenerateTokensAsync(
        Guid userId,
        string email,
        string fullName,
        IReadOnlyList<string> roles,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default)
    {
        string accessToken = GenerateAccessToken(userId, email, fullName, roles);
        int expiresInSeconds = _options.AccessTokenExpirationMinutes * 60;

        string rawRefreshToken = GenerateSecureRandomToken();
        string tokenHash = HashToken(rawRefreshToken);

        DateTimeOffset now = _timeProvider.GetUtcNow();
        DateTimeOffset refreshExpiresAt = now.AddDays(_options.RefreshTokenExpirationDays);
        Guid familyId = Guid.NewGuid();

        RefreshToken refreshToken = new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            FamilyId = familyId,
            ExpiresAt = refreshExpiresAt,
            CreatedAt = now,
            CreatedByIp = ipAddress,
            UserAgent = userAgent
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(ct);

        return new GeneratedTokens(
            accessToken,
            expiresInSeconds,
            rawRefreshToken,
            refreshExpiresAt,
            familyId);
    }

    public async Task<Result<RefreshResult>> RotateRefreshTokenAsync(
        string rawRefreshToken,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return Result.Failure<RefreshResult>(
                DomainError.Unauthorized(ErrorCodes.RefreshTokenInvalid, "Refresh token is required."));
        }

        string tokenHash = HashToken(rawRefreshToken);
        DateTimeOffset now = _timeProvider.GetUtcNow();

        RefreshToken? existingToken = await _dbContext.RefreshTokens
            .Include(r => r.User)
            .ThenInclude(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, ct);

        if (existingToken is null)
        {
            return Result.Failure<RefreshResult>(
                DomainError.Unauthorized(ErrorCodes.RefreshTokenInvalid, "Invalid refresh token."));
        }

        // Reuse detection: token was already replaced or revoked
        if (existingToken.ReplacedByTokenId is not null || existingToken.RevokedAt is not null)
        {
            // Compromise detected: revoke all tokens in the entire family
            List<RefreshToken> familyTokens = await _dbContext.RefreshTokens
                .Where(r => r.FamilyId == existingToken.FamilyId && r.RevokedAt == null)
                .ToListAsync(ct);

            foreach (RefreshToken token in familyTokens)
            {
                token.RevokedAt = now;
            }

            await _dbContext.SaveChangesAsync(ct);

            return Result.Failure<RefreshResult>(
                DomainError.Unauthorized(ErrorCodes.RefreshTokenInvalid, "Refresh token reuse detected."));
        }

        if (existingToken.ExpiresAt <= now)
        {
            return Result.Failure<RefreshResult>(
                DomainError.Unauthorized(ErrorCodes.RefreshTokenInvalid, "Refresh token has expired."));
        }

        if (existingToken.User.Status != UserStatus.Active || existingToken.User.DeletedAt is not null)
        {
            return Result.Failure<RefreshResult>(
                DomainError.Forbidden(ErrorCodes.AccountLocked, "User account is locked or disabled."));
        }

        // Issue replacement token in the same family
        string newRawRefreshToken = GenerateSecureRandomToken();
        string newTokenHash = HashToken(newRawRefreshToken);
        DateTimeOffset newExpiresAt = now.AddDays(_options.RefreshTokenExpirationDays);

        RefreshToken replacementToken = new()
        {
            Id = Guid.NewGuid(),
            UserId = existingToken.UserId,
            TokenHash = newTokenHash,
            FamilyId = existingToken.FamilyId,
            ExpiresAt = newExpiresAt,
            CreatedAt = now,
            CreatedByIp = ipAddress,
            UserAgent = userAgent
        };

        existingToken.RevokedAt = now;
        existingToken.ReplacedByTokenId = replacementToken.Id;

        _dbContext.RefreshTokens.Add(replacementToken);
        await _dbContext.SaveChangesAsync(ct);

        IReadOnlyList<string> roles = existingToken.User.UserRoles
            .Select(ur => ur.Role.Name)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        string newAccessToken = GenerateAccessToken(
            existingToken.UserId,
            existingToken.User.Email,
            existingToken.User.FullName,
            roles);

        int expiresInSeconds = _options.AccessTokenExpirationMinutes * 60;

        GeneratedTokens generated = new(
            newAccessToken,
            expiresInSeconds,
            newRawRefreshToken,
            newExpiresAt,
            existingToken.FamilyId);

        return Result.Success(new RefreshResult(generated, existingToken.UserId));
    }

    public async Task<Result> RevokeFamilyAsync(
        string rawRefreshToken,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return Result.Success();
        }

        string tokenHash = HashToken(rawRefreshToken);
        DateTimeOffset now = _timeProvider.GetUtcNow();

        RefreshToken? token = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, ct);

        if (token is null)
        {
            return Result.Success();
        }

        List<RefreshToken> familyTokens = await _dbContext.RefreshTokens
            .Where(r => r.FamilyId == token.FamilyId && r.RevokedAt == null)
            .ToListAsync(ct);

        foreach (RefreshToken t in familyTokens)
        {
            t.RevokedAt = now;
        }

        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task RevokeAllUserTokensAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();

        List<RefreshToken> activeTokens = await _dbContext.RefreshTokens
            .Where(r => r.UserId == userId && r.RevokedAt == null)
            .ToListAsync(ct);

        foreach (RefreshToken t in activeTokens)
        {
            t.RevokedAt = now;
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task RevokeOtherFamiliesAsync(
        Guid userId,
        Guid keepFamilyId,
        CancellationToken ct = default)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();

        List<RefreshToken> otherTokens = await _dbContext.RefreshTokens
            .Where(r => r.UserId == userId && r.FamilyId != keepFamilyId && r.RevokedAt == null)
            .ToListAsync(ct);

        foreach (RefreshToken t in otherTokens)
        {
            t.RevokedAt = now;
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    public static string HashToken(string rawToken)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(rawToken);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static string GenerateSecureRandomToken()
    {
        byte[] randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .TrimEnd('=');
    }
}
