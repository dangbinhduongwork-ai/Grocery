namespace GroceryHub.Modules.Identity.Contracts;

public sealed record AuthUserDto(
    Guid Id,
    string Email,
    string FullName,
    string? Phone,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    string? Phone);

public sealed record RegisterResponse(
    Guid UserId,
    string Email,
    bool VerificationRequired);

public sealed record LoginRequest(
    string Email,
    string Password);

public sealed record AuthTokenResponse(
    string AccessToken,
    int ExpiresIn,
    AuthUserDto User);

public sealed record VerifyEmailRequest(
    Guid UserId,
    string Token);

public sealed record VerifyEmailResponse(
    bool Verified);

public sealed record ResendVerificationRequest(
    string Email);

public sealed record AcceptedResponse(
    bool Accepted);

public sealed record ForgotPasswordRequest(
    string Email);

public sealed record ResetPasswordRequest(
    Guid UserId,
    string Token,
    string NewPassword);

public sealed record ResetPasswordResponse(
    bool Reset);

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);

public sealed record ChangePasswordResponse(
    bool Changed);

public sealed record StaffAccountDto(
    Guid UserId,
    string EmployeeCode,
    string Email,
    string FullName,
    string? Phone,
    string? Position,
    string Status,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> ExtraPermissions,
    DateTimeOffset HiredAt);

public sealed record CreateStaffRequest(
    string Email,
    string Password,
    string FullName,
    string? Phone,
    string EmployeeCode,
    string? Position,
    string Role,
    IReadOnlyList<string>? ExtraPermissions);

public sealed record UpdateStaffRequest(
    string FullName,
    string? Phone,
    string? Position,
    string Status,
    string? Note);

public sealed record UpdateStaffRolesRequest(
    IReadOnlyList<string> Roles);

public sealed record UpdateStaffPermissionsRequest(
    IReadOnlyList<string> ExtraPermissions);

public sealed record RolePermissionMatrixDto(
    Guid RoleId,
    string Name,
    bool IsImmutableAdmin,
    IReadOnlyList<string> Permissions);

public sealed record UpdateRolePermissionsRequest(
    IReadOnlyList<string> Permissions);

public sealed record PermissionDefinitionDto(
    string Code,
    string Group,
    string Description,
    bool AllowUserGrant);
