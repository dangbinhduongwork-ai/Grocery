using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GroceryHub.Modules.Identity.Application;
using GroceryHub.Modules.Identity.Configuration;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace GroceryHub.UnitTests.Identity;

public sealed class TokenServiceTests : IDisposable
{
    private readonly GroceryHubDbContext _dbContext;
    private readonly FakeTimeProvider _timeProvider;
    private readonly IOptions<JwtOptions> _jwtOptions;
    private readonly TokenService _tokenService;

    private const string SecretKey = "test-super-secret-key-that-is-at-least-256-bits-long-for-hmac-sha256!";
    private const string Issuer = "GroceryHubTestIssuer";
    private const string Audience = "GroceryHubTestAudience";

    public TokenServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<GroceryHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new GroceryHubDbContext(dbOptions);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 11, 15, 12, 0, 0, TimeSpan.Zero));

        var options = new JwtOptions
        {
            SecretKey = SecretKey,
            Issuer = Issuer,
            Audience = Audience,
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        };
        _jwtOptions = Options.Create(options);

        _tokenService = new TokenService(_dbContext, _jwtOptions, _timeProvider);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public void GenerateAccessToken_ProducesValidJwtWithExpectedClaims()
    {
        var userId = Guid.NewGuid();
        var email = "customer@groceryhub.vn";
        var fullName = "Nguyen Van A";
        var roles = new[] { "Customer" };

        var token = _tokenService.GenerateAccessToken(userId, email, fullName, roles);

        Assert.NotNull(token);

        // Validate token structure and claims
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            LifetimeValidator = (notBefore, expires, _, _) =>
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                return notBefore <= now && expires > now;
            }
        };

        var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
        var jwtToken = validatedToken as JwtSecurityToken;
        Assert.NotNull(jwtToken);

        // Claims checks
        Assert.Equal(userId.ToString(), principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal(email, principal.FindFirstValue(ClaimTypes.Email));
        Assert.Equal(fullName, principal.FindFirstValue(ClaimTypes.Name));
        Assert.Contains(principal.FindAll(ClaimTypes.Role), c => c.Value == "Customer");
        Assert.NotNull(jwtToken.Id); // jti exists

        // Invariant: No permissions in JWT claims (ADR-003)
        Assert.DoesNotContain(principal.Claims, c => c.Type.Equals("permission", StringComparison.OrdinalIgnoreCase) ||
                                                    c.Type.Equals("permissions", StringComparison.OrdinalIgnoreCase));

        // Invariant: Expiration matches 15 minutes from now
        var expectedExpiration = _timeProvider.GetUtcNow().AddMinutes(15).UtcDateTime;
        Assert.Equal(expectedExpiration, jwtToken.ValidTo, precision: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GenerateTokensAsync_CreatesTokenInDatabaseWithExpectedFamilyAndHash()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "staff@groceryhub.vn",
            FullName = "Tran Thi B",
            Status = UserStatus.Active
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var tokens = await _tokenService.GenerateTokensAsync(user.Id, user.Email, user.FullName, ["Staff"], "127.0.0.1", "TestAgent");

        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RawRefreshToken));
        Assert.NotEqual(Guid.Empty, tokens.FamilyId);
        Assert.Equal(15 * 60, tokens.ExpiresIn);
        Assert.Equal(_timeProvider.GetUtcNow().AddDays(7), tokens.RefreshTokenExpiresAt);

        // Record exists in DB
        var savedRecord = await _dbContext.RefreshTokens.FirstOrDefaultAsync(r => r.FamilyId == tokens.FamilyId);
        Assert.NotNull(savedRecord);
        Assert.Equal(user.Id, savedRecord.UserId);
        Assert.Null(savedRecord.RevokedAt);
        Assert.Null(savedRecord.ReplacedByTokenId);
        Assert.NotEqual(tokens.RawRefreshToken, savedRecord.TokenHash); // Token is hashed
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_ValidToken_IssuesNewTokenInSameFamily()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "customer@groceryhub.vn",
            FullName = "Nguyen Van A",
            Status = UserStatus.Active
        };
        var role = new Role { Id = Guid.NewGuid(), Name = "Customer" };
        _dbContext.Users.Add(user);
        _dbContext.Roles.Add(role);
        _dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await _dbContext.SaveChangesAsync();

        var tokens1 = await _tokenService.GenerateTokensAsync(user.Id, user.Email, user.FullName, ["Customer"], "127.0.0.1", "Browser 1");

        // Advance time by 1 day
        _timeProvider.Advance(TimeSpan.FromDays(1));

        var rotateResult = await _tokenService.RotateRefreshTokenAsync(tokens1.RawRefreshToken, "127.0.0.1", "Browser 1");

        Assert.True(rotateResult.IsSuccess);
        var refreshResult = rotateResult.Value;

        Assert.NotNull(refreshResult.Tokens.RawRefreshToken);
        Assert.NotEqual(tokens1.RawRefreshToken, refreshResult.Tokens.RawRefreshToken);
        Assert.Equal(user.Id, refreshResult.UserId);
        Assert.Equal(tokens1.FamilyId, refreshResult.Tokens.FamilyId);

        // Verify old token is now marked as revoked and replaced
        var oldToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(r => r.ReplacedByTokenId != null);
        Assert.NotNull(oldToken);
        Assert.NotNull(oldToken.RevokedAt);
        Assert.Equal(_timeProvider.GetUtcNow(), oldToken.RevokedAt);

        // Verify new token exists with same family
        var newToken = await _dbContext.RefreshTokens.FindAsync(oldToken.ReplacedByTokenId!.Value);
        Assert.NotNull(newToken);
        Assert.Equal(tokens1.FamilyId, newToken.FamilyId);
        Assert.Null(newToken.RevokedAt);
        Assert.Null(newToken.ReplacedByTokenId);
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_ReuseDetected_RevokesEntireTokenFamily()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "victim@groceryhub.vn",
            FullName = "Le Van C",
            Status = UserStatus.Active
        };
        var role = new Role { Id = Guid.NewGuid(), Name = "Customer" };
        _dbContext.Users.Add(user);
        _dbContext.Roles.Add(role);
        _dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await _dbContext.SaveChangesAsync();

        // 1. Initial login: issues T1
        var tokens1 = await _tokenService.GenerateTokensAsync(user.Id, user.Email, user.FullName, ["Customer"], "1.1.1.1", "Legitimate Browser");

        // 2. Legitimate user refreshes: rotates T1 -> T2
        var rotate1Result = await _tokenService.RotateRefreshTokenAsync(tokens1.RawRefreshToken, "1.1.1.1", "Legitimate Browser");
        Assert.True(rotate1Result.IsSuccess);
        var tokens2 = rotate1Result.Value.Tokens;

        // 3. Legitimate user rotates again: rotates T2 -> T3
        var rotate2Result = await _tokenService.RotateRefreshTokenAsync(tokens2.RawRefreshToken, "1.1.1.1", "Legitimate Browser");
        Assert.True(rotate2Result.IsSuccess);

        // Verify active token count in family is 1 (only T3 is active)
        var activeTokensBeforeAttack = await _dbContext.RefreshTokens
            .Where(t => t.FamilyId == tokens1.FamilyId && t.RevokedAt == null)
            .CountAsync();
        Assert.Equal(1, activeTokensBeforeAttack);

        // 4. ATTACK: Attacker attempts to replay the already-replaced token T1!
        _timeProvider.Advance(TimeSpan.FromHours(1));
        var replayResult = await _tokenService.RotateRefreshTokenAsync(tokens1.RawRefreshToken, "2.2.2.2", "Malicious Actor");

        // Invariant: Must fail with REFRESH_TOKEN_INVALID
        Assert.True(replayResult.IsFailure);
        Assert.Equal(ErrorCodes.RefreshTokenInvalid, replayResult.Error.Code);

        // Invariant: ENTIRE family must be immediately revoked to protect the victim
        var activeTokensAfterAttack = await _dbContext.RefreshTokens
            .Where(t => t.FamilyId == tokens1.FamilyId && t.RevokedAt == null)
            .CountAsync();
        Assert.Equal(0, activeTokensAfterAttack);

        // Even T3 (which was active) is now revoked!
        var allTokensInFamily = await _dbContext.RefreshTokens
            .Where(t => t.FamilyId == tokens1.FamilyId)
            .ToListAsync();
        Assert.All(allTokensInFamily, t => Assert.NotNull(t.RevokedAt));
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_ExpiredToken_ReturnsFailure()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@groceryhub.vn",
            FullName = "User Test",
            Status = UserStatus.Active
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var tokens = await _tokenService.GenerateTokensAsync(user.Id, user.Email, user.FullName, ["Customer"], "127.0.0.1", "TestClient");

        // Advance past 7 days expiration
        _timeProvider.Advance(TimeSpan.FromDays(8));

        var rotateResult = await _tokenService.RotateRefreshTokenAsync(tokens.RawRefreshToken, "127.0.0.1", "TestClient");

        Assert.True(rotateResult.IsFailure);
        Assert.Equal(ErrorCodes.RefreshTokenInvalid, rotateResult.Error.Code);
    }

    [Fact]
    public async Task RevokeFamilyAsync_RevokesAllTokensInFamily()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@groceryhub.vn",
            FullName = "User Test",
            Status = UserStatus.Active
        };
        var role = new Role { Id = Guid.NewGuid(), Name = "Customer" };
        _dbContext.Users.Add(user);
        _dbContext.Roles.Add(role);
        _dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await _dbContext.SaveChangesAsync();

        var tokens1 = await _tokenService.GenerateTokensAsync(user.Id, user.Email, user.FullName, ["Customer"], "127.0.0.1", "TestClient");
        var rotateResult = await _tokenService.RotateRefreshTokenAsync(tokens1.RawRefreshToken, "127.0.0.1", "TestClient");
        Assert.True(rotateResult.IsSuccess);

        var revokeResult = await _tokenService.RevokeFamilyAsync(rotateResult.Value.Tokens.RawRefreshToken);
        Assert.True(revokeResult.IsSuccess);

        var activeCount = await _dbContext.RefreshTokens
            .Where(t => t.FamilyId == tokens1.FamilyId && t.RevokedAt == null)
            .CountAsync();
        Assert.Equal(0, activeCount);
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_RevokesAllTokensForUser()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@groceryhub.vn",
            FullName = "User Test",
            Status = UserStatus.Active
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Two distinct sessions / families
        var tokens1 = await _tokenService.GenerateTokensAsync(user.Id, user.Email, user.FullName, ["Customer"], "127.0.0.1", "Device 1");
        var tokens2 = await _tokenService.GenerateTokensAsync(user.Id, user.Email, user.FullName, ["Customer"], "127.0.0.1", "Device 2");

        Assert.NotEqual(tokens1.FamilyId, tokens2.FamilyId);

        await _tokenService.RevokeAllUserTokensAsync(user.Id);

        var activeCount = await _dbContext.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .CountAsync();
        Assert.Equal(0, activeCount);
    }
}
