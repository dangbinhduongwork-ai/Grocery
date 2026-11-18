using System.Security.Cryptography;
using System.Text;
using GroceryHub.Modules.Identity.Contracts;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GroceryHub.Modules.Identity.Application;

public sealed class AuthService
{
    public const string RefreshCookieName = "gh_refresh";
    public const string RefreshCookiePath = "/api/v1/auth";

    private readonly GroceryHubDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly IPasswordPolicyValidator _passwordValidator;
    private readonly IEmailService _emailService;
    private readonly IPermissionService _permissionService;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly TimeProvider _timeProvider;
    private readonly ICartMergeService? _cartMergeService;

    public AuthService(
        GroceryHubDbContext dbContext,
        ITokenService tokenService,
        IPasswordPolicyValidator passwordValidator,
        IEmailService emailService,
        IPermissionService permissionService,
        IPasswordHasher<User> passwordHasher,
        TimeProvider timeProvider,
        ICartMergeService? cartMergeService = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _passwordValidator = passwordValidator ?? throw new ArgumentNullException(nameof(passwordValidator));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _cartMergeService = cartMergeService;
    }

    public async Task<Result<RegisterResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Result.Failure<RegisterResponse>(DomainError.ValidationFailed(
                new Dictionary<string, string[]> { ["email"] = ["Email is required."] },
                "Email is required."));
        }

        string email = request.Email.Trim().ToLowerInvariant();
        string normalizedEmail = email.ToUpperInvariant();

        Result passwordValidation = _passwordValidator.Validate(request.Password);
        if (passwordValidation.IsFailure)
        {
            return Result.Failure<RegisterResponse>(passwordValidation.Error);
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return Result.Failure<RegisterResponse>(DomainError.ValidationFailed(
                new Dictionary<string, string[]> { ["fullName"] = ["Full name is required."] },
                "Full name is required."));
        }

        string? phone = null;
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            phone = request.Phone.Trim();
            if (!VietnamesePhone.IsValid(phone))
            {
                return Result.Failure<RegisterResponse>(DomainError.ValidationFailed(
                    new Dictionary<string, string[]> { ["phone"] = ["Phone number must be a valid 10-digit Vietnamese mobile number."] },
                    "Invalid Vietnamese phone number."));
            }
        }

        bool emailExists = await _dbContext.Users
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail, ct);

        if (emailExists)
        {
            return Result.Failure<RegisterResponse>(DomainError.Conflict(
                ErrorCodes.EmailAlreadyRegistered,
                "Email is already registered."));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        User user = new()
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = normalizedEmail,
            FullName = request.FullName.Trim(),
            PhoneNumber = phone,
            Status = UserStatus.Active,
            EmailConfirmed = false,
            LockoutEnabled = true,
            AccessFailedCount = 0,
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = now
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        Role? customerRole = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Name == PredefinedRoles.Customer, ct);

        if (customerRole is not null)
        {
            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = customerRole.Id,
                CreatedAt = now
            });
        }

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(ct);

        string verificationToken = GenerateTimedOpaqueToken(user.Id, user.SecurityStamp!, _timeProvider.GetUtcNow().AddHours(24));
        await _emailService.SendVerificationEmailAsync(user.Id, user.Email, verificationToken, ct);

        return Result.Success(new RegisterResponse(user.Id, user.Email, VerificationRequired: true));
    }

    public async Task<Result<VerifyEmailResponse>> VerifyEmailAsync(
        VerifyEmailRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        User? user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct);

        if (user is null)
        {
            return Result.Failure<VerifyEmailResponse>(DomainError.NotFound(
                $"User with ID '{request.UserId}' was not found."));
        }

        if (user.EmailConfirmed)
        {
            return Result.Success(new VerifyEmailResponse(Verified: true));
        }

        if (!ValidateTimedOpaqueToken(request.Token, user.Id, user.SecurityStamp!))
        {
            return Result.Failure<VerifyEmailResponse>(DomainError.ValidationFailed(
                new Dictionary<string, string[]> { ["token"] = ["Verification token is invalid or expired."] },
                "Invalid or expired verification token."));
        }

        user.EmailConfirmed = true;
        user.SecurityStamp = Guid.NewGuid().ToString();
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new VerifyEmailResponse(Verified: true));
    }

    public async Task<AcceptedResponse> ResendVerificationAsync(
        ResendVerificationRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            string normalized = request.Email.Trim().ToUpperInvariant();
            User? user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, ct);

            if (user is not null && !user.EmailConfirmed && user.Status == UserStatus.Active && user.DeletedAt is null)
            {
                string token = GenerateTimedOpaqueToken(user.Id, user.SecurityStamp ??= Guid.NewGuid().ToString(), _timeProvider.GetUtcNow().AddHours(24));
                await _emailService.SendVerificationEmailAsync(user.Id, user.Email, token, ct);
            }
        }

        // Anti-enumeration: always return accepted
        return new AcceptedResponse(Accepted: true);
    }

    public async Task<Result<AuthTokenResponse>> LoginAsync(
        LoginRequest request,
        HttpResponse httpResponse,
        string? ipAddress = null,
        string? userAgent = null,
        string? guestCartToken = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(httpResponse);

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result.Failure<AuthTokenResponse>(DomainError.Unauthorized(
                ErrorCodes.Unauthenticated,
                "Invalid email or password."));
        }

        string normalizedEmail = request.Email.Trim().ToUpperInvariant();
        DateTimeOffset now = _timeProvider.GetUtcNow();

        User? user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct);

        if (user is null)
        {
            return Result.Failure<AuthTokenResponse>(DomainError.Unauthorized(
                ErrorCodes.Unauthenticated,
                "Invalid email or password."));
        }

        // Lockout check
        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > now)
        {
            return Result.Failure<AuthTokenResponse>(new DomainError(
                ErrorCodes.AccountLocked,
                "Account is locked due to too many failed attempts. Please try again later.",
                (ErrorType)423));
        }

        PasswordVerificationResult verifyResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verifyResult == PasswordVerificationResult.Failed)
        {
            user.AccessFailedCount++;
            if (user.LockoutEnabled && user.AccessFailedCount >= 5)
            {
                user.LockoutEnd = now.AddMinutes(15);
            }

            await _dbContext.SaveChangesAsync(ct);

            return Result.Failure<AuthTokenResponse>(DomainError.Unauthorized(
                ErrorCodes.Unauthenticated,
                "Invalid email or password."));
        }

        if (!user.EmailConfirmed)
        {
            return Result.Failure<AuthTokenResponse>(DomainError.Forbidden(
                ErrorCodes.EmailNotVerified,
                "Email address has not been verified. Please check your inbox for the verification link."));
        }

        if (user.Status != UserStatus.Active || user.DeletedAt is not null)
        {
            return Result.Failure<AuthTokenResponse>(DomainError.Forbidden(
                ErrorCodes.AccountLocked,
                "User account is locked or disabled."));
        }

        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
        await _dbContext.SaveChangesAsync(ct);

        IReadOnlyList<string> roles = user.UserRoles
            .Select(ur => ur.Role.Name)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        GeneratedTokens tokens = await _tokenService.GenerateTokensAsync(
            user.Id,
            user.Email,
            user.FullName,
            roles,
            ipAddress,
            userAgent,
            ct);

        Result<IReadOnlySet<string>> permissionsResult = await _permissionService.GetEffectivePermissionsAsync(user.Id, ct);
        IReadOnlyList<string> permissions = permissionsResult.IsSuccess
            ? permissionsResult.Value.OrderBy(p => p, StringComparer.Ordinal).ToList()
            : [];

        SetRefreshCookie(httpResponse, tokens.RawRefreshToken, tokens.RefreshTokenExpiresAt);

        if (!string.IsNullOrWhiteSpace(guestCartToken) && _cartMergeService != null)
        {
            await _cartMergeService.MergeGuestCartOnLoginAsync(user.Id, guestCartToken, ct);
            httpResponse.Cookies.Delete("gh_cart");
        }

        AuthUserDto authUser = new(
            user.Id,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            roles,
            permissions);

        return Result.Success(new AuthTokenResponse(tokens.AccessToken, tokens.ExpiresIn, authUser));
    }

    public async Task<Result<AuthTokenResponse>> ActivateAndLoginAsync(
        LoginRequest request,
        HttpResponse httpResponse,
        string? ipAddress = null,
        string? userAgent = null,
        string? guestCartToken = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(httpResponse);

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result.Failure<AuthTokenResponse>(DomainError.Unauthorized(
                ErrorCodes.Unauthenticated,
                "Invalid email or password."));
        }

        string normalizedEmail = request.Email.Trim().ToUpperInvariant();
        DateTimeOffset now = _timeProvider.GetUtcNow();

        User? user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct);

        if (user is null)
        {
            return Result.Failure<AuthTokenResponse>(DomainError.Unauthorized(
                ErrorCodes.Unauthenticated,
                "Invalid email or password."));
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > now)
        {
            return Result.Failure<AuthTokenResponse>(new DomainError(
                ErrorCodes.AccountLocked,
                "Account is locked due to too many failed attempts. Please try again later.",
                (ErrorType)423));
        }

        PasswordVerificationResult verifyResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verifyResult == PasswordVerificationResult.Failed)
        {
            user.AccessFailedCount++;
            if (user.LockoutEnabled && user.AccessFailedCount >= 5)
            {
                user.LockoutEnd = now.AddMinutes(15);
            }

            await _dbContext.SaveChangesAsync(ct);

            return Result.Failure<AuthTokenResponse>(DomainError.Unauthorized(
                ErrorCodes.Unauthenticated,
                "Invalid email or password."));
        }

        user.EmailConfirmed = true;
        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
        await _dbContext.SaveChangesAsync(ct);

        IReadOnlyList<string> roles = user.UserRoles
            .Select(ur => ur.Role.Name)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        GeneratedTokens tokens = await _tokenService.GenerateTokensAsync(
            user.Id,
            user.Email,
            user.FullName,
            roles,
            ipAddress,
            userAgent,
            ct);

        Result<IReadOnlySet<string>> permissionsResult = await _permissionService.GetEffectivePermissionsAsync(user.Id, ct);
        IReadOnlyList<string> permissions = permissionsResult.IsSuccess
            ? permissionsResult.Value.OrderBy(p => p, StringComparer.Ordinal).ToList()
            : [];

        SetRefreshCookie(httpResponse, tokens.RawRefreshToken, tokens.RefreshTokenExpiresAt);

        if (!string.IsNullOrWhiteSpace(guestCartToken) && _cartMergeService != null)
        {
            await _cartMergeService.MergeGuestCartOnLoginAsync(user.Id, guestCartToken, ct);
            httpResponse.Cookies.Delete("gh_cart");
        }

        AuthUserDto authUser = new(
            user.Id,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            roles,
            permissions);

        return Result.Success(new AuthTokenResponse(tokens.AccessToken, tokens.ExpiresIn, authUser));
    }

    public async Task<Result<AuthTokenResponse>> RefreshAsync(
        HttpRequest httpRequest,
        HttpResponse httpResponse,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(httpRequest);
        ArgumentNullException.ThrowIfNull(httpResponse);

        bool hasXrw = httpRequest.Headers.TryGetValue("X-Requested-With", out var xrw) &&
            string.Equals(xrw.ToString(), "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        bool hasJsonHeader = httpRequest.Headers.TryGetValue("Content-Type", out var ctHeader) &&
            ctHeader.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);

        if (!hasXrw && !hasJsonHeader)
        {
            return Result.Failure<AuthTokenResponse>(DomainError.BadRequest(
                ErrorCodes.BadRequest,
                "Missing or invalid X-Requested-With or Content-Type header."));
        }

        if (!httpRequest.Cookies.TryGetValue(RefreshCookieName, out string? rawRefreshToken) ||
            string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return Result.Failure<AuthTokenResponse>(DomainError.Unauthorized(
                ErrorCodes.RefreshTokenInvalid,
                "Refresh token cookie is missing."));
        }

        Result<RefreshResult> rotationResult = await _tokenService.RotateRefreshTokenAsync(
            rawRefreshToken,
            ipAddress,
            userAgent,
            ct);

        if (rotationResult.IsFailure)
        {
            ClearRefreshCookie(httpResponse);
            return Result.Failure<AuthTokenResponse>(rotationResult.Error);
        }

        RefreshResult result = rotationResult.Value;

        User? user = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == result.UserId, ct);

        if (user is null || user.Status != UserStatus.Active || user.DeletedAt is not null)
        {
            ClearRefreshCookie(httpResponse);
            return Result.Failure<AuthTokenResponse>(DomainError.Forbidden(
                ErrorCodes.AccountLocked,
                "User account is locked or disabled."));
        }

        IReadOnlyList<string> roles = user.UserRoles
            .Select(ur => ur.Role.Name)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Result<IReadOnlySet<string>> permissionsResult = await _permissionService.GetEffectivePermissionsAsync(user.Id, ct);
        IReadOnlyList<string> permissions = permissionsResult.IsSuccess
            ? permissionsResult.Value.OrderBy(p => p, StringComparer.Ordinal).ToList()
            : [];

        SetRefreshCookie(httpResponse, result.Tokens.RawRefreshToken, result.Tokens.RefreshTokenExpiresAt);

        AuthUserDto authUser = new(
            user.Id,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            roles,
            permissions);

        return Result.Success(new AuthTokenResponse(result.Tokens.AccessToken, result.Tokens.ExpiresIn, authUser));
    }

    public async Task<Result> LogoutAsync(
        HttpRequest httpRequest,
        HttpResponse httpResponse,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(httpRequest);
        ArgumentNullException.ThrowIfNull(httpResponse);

        if (httpRequest.Cookies.TryGetValue(RefreshCookieName, out string? rawRefreshToken) &&
            !string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            await _tokenService.RevokeFamilyAsync(rawRefreshToken, ct);
        }

        ClearRefreshCookie(httpResponse);
        return Result.Success();
    }

    public async Task<AcceptedResponse> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            string normalized = request.Email.Trim().ToUpperInvariant();
            User? user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, ct);

            if (user is not null && user.Status == UserStatus.Active && user.DeletedAt is null)
            {
                string token = GenerateTimedOpaqueToken(user.Id, user.SecurityStamp ??= Guid.NewGuid().ToString(), _timeProvider.GetUtcNow().AddHours(1));
                await _emailService.SendPasswordResetEmailAsync(user.Id, user.Email, token, ct);
            }
        }

        return new AcceptedResponse(Accepted: true);
    }

    public async Task<Result<ResetPasswordResponse>> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        User? user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct);

        if (user is null || user.DeletedAt is not null || !ValidateTimedOpaqueToken(request.Token, user.Id, user.SecurityStamp!))
        {
            return Result.Failure<ResetPasswordResponse>(DomainError.ValidationFailed(
                new Dictionary<string, string[]> { ["token"] = ["Password reset token is invalid or expired."] },
                "Invalid password reset token."));
        }

        Result passwordValidation = _passwordValidator.Validate(request.NewPassword);
        if (passwordValidation.IsFailure)
        {
            return Result.Failure<ResetPasswordResponse>(passwordValidation.Error);
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.AccessFailedCount = 0;
        user.LockoutEnd = null;

        await _dbContext.SaveChangesAsync(ct);

        await _tokenService.RevokeAllUserTokensAsync(user.Id, ct);
        _permissionService.InvalidateUserPermissionsCache(user.Id);

        return Result.Success(new ResetPasswordResponse(Reset: true));
    }

    public async Task<Result<ChangePasswordResponse>> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        HttpRequest httpRequest,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(httpRequest);

        User? user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null || user.Status != UserStatus.Active || user.DeletedAt is not null)
        {
            return Result.Failure<ChangePasswordResponse>(DomainError.Unauthorized(
                ErrorCodes.Unauthenticated,
                "User is not authenticated."));
        }

        PasswordVerificationResult verifyResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.CurrentPassword);

        if (verifyResult == PasswordVerificationResult.Failed)
        {
            return Result.Failure<ChangePasswordResponse>(DomainError.Unauthorized(
                ErrorCodes.Unauthenticated,
                "Current password is incorrect."));
        }

        Result passwordValidation = _passwordValidator.Validate(request.NewPassword);
        if (passwordValidation.IsFailure)
        {
            return Result.Failure<ChangePasswordResponse>(passwordValidation.Error);
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString();
        await _dbContext.SaveChangesAsync(ct);

        // Revoke other refresh token families
        if (httpRequest.Cookies.TryGetValue(RefreshCookieName, out string? rawToken) &&
            !string.IsNullOrWhiteSpace(rawToken))
        {
            string hash = TokenService.HashToken(rawToken);
            RefreshToken? currentRefreshToken = await _dbContext.RefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.TokenHash == hash, ct);

            if (currentRefreshToken is not null)
            {
                await _tokenService.RevokeOtherFamiliesAsync(userId, currentRefreshToken.FamilyId, ct);
            }
            else
            {
                await _tokenService.RevokeAllUserTokensAsync(userId, ct);
            }
        }
        else
        {
            await _tokenService.RevokeAllUserTokensAsync(userId, ct);
        }

        _permissionService.InvalidateUserPermissionsCache(userId);

        return Result.Success(new ChangePasswordResponse(Changed: true));
    }

    public async Task<Result<AuthUserDto>> GetMeAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        User? user = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null || user.DeletedAt is not null)
        {
            return Result.Failure<AuthUserDto>(DomainError.Unauthorized(
                ErrorCodes.Unauthenticated,
                "User was not found."));
        }

        if (user.Status != UserStatus.Active)
        {
            return Result.Failure<AuthUserDto>(DomainError.Forbidden(
                ErrorCodes.AccountLocked,
                "User account is locked or disabled."));
        }

        IReadOnlyList<string> roles = user.UserRoles
            .Select(ur => ur.Role.Name)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Result<IReadOnlySet<string>> permissionsResult = await _permissionService.GetEffectivePermissionsAsync(user.Id, ct);
        IReadOnlyList<string> permissions = permissionsResult.IsSuccess
            ? permissionsResult.Value.OrderBy(p => p, StringComparer.Ordinal).ToList()
            : [];

        AuthUserDto authUser = new(
            user.Id,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            roles,
            permissions);

        return Result.Success(authUser);
    }

    private static void SetRefreshCookie(HttpResponse response, string rawToken, DateTimeOffset expiresAt)
    {
        CookieOptions cookieOptions = new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
            Expires = expiresAt
        };

        response.Cookies.Append(RefreshCookieName, rawToken, cookieOptions);
    }

    private static void ClearRefreshCookie(HttpResponse response)
    {
        CookieOptions cookieOptions = new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
            Expires = DateTimeOffset.UnixEpoch
        };

        response.Cookies.Append(RefreshCookieName, string.Empty, cookieOptions);
    }

    private string GenerateTimedOpaqueToken(Guid userId, string securityStamp, DateTimeOffset expiresAt)
    {
        long exp = expiresAt.ToUnixTimeSeconds();
        string raw = $"{userId:N}:{exp}:{securityStamp}";
        byte[] bytes = Encoding.UTF8.GetBytes(raw);
        byte[] hash = SHA256.HashData(bytes);
        return $"{exp}.{Convert.ToHexString(hash)}";
    }

    private bool ValidateTimedOpaqueToken(string providedToken, Guid userId, string securityStamp)
    {
        if (string.IsNullOrWhiteSpace(providedToken))
        {
            return false;
        }

        string[] parts = providedToken.Split('.');
        if (parts.Length != 2 || !long.TryParse(parts[0], out long exp))
        {
            return ValidateOpaqueToken(providedToken, userId, securityStamp);
        }

        if (_timeProvider.GetUtcNow().ToUnixTimeSeconds() > exp)
        {
            return false;
        }

        string expected = GenerateTimedOpaqueToken(userId, securityStamp, DateTimeOffset.FromUnixTimeSeconds(exp));
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedToken),
            Encoding.UTF8.GetBytes(expected));
    }

    private static string GenerateOpaqueToken(Guid userId, string securityStamp)
    {
        string raw = $"{userId:N}:{securityStamp}";
        byte[] bytes = Encoding.UTF8.GetBytes(raw);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static bool ValidateOpaqueToken(string providedToken, Guid userId, string securityStamp)
    {
        if (string.IsNullOrWhiteSpace(providedToken))
        {
            return false;
        }

        string expectedToken = GenerateOpaqueToken(userId, securityStamp);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedToken),
            Encoding.UTF8.GetBytes(expectedToken));
    }
}
