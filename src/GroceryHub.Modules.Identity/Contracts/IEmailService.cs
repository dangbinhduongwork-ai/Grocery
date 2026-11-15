namespace GroceryHub.Modules.Identity.Contracts;

public interface IEmailService
{
    Task SendVerificationEmailAsync(
        Guid userId,
        string email,
        string token,
        CancellationToken ct = default);

    Task SendPasswordResetEmailAsync(
        Guid userId,
        string email,
        string token,
        CancellationToken ct = default);
}
