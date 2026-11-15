using GroceryHub.Modules.Identity.Contracts;
using Microsoft.Extensions.Logging;

namespace GroceryHub.Modules.Identity.Application;

public sealed class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task SendVerificationEmailAsync(
        Guid userId,
        string email,
        string token,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        _logger.LogInformation(
            "Sending verification email to {Email} for UserId {UserId} with token {Token}",
            email,
            userId,
            token);

        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(
        Guid userId,
        string email,
        string token,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        _logger.LogInformation(
            "Sending password reset email to {Email} for UserId {UserId} with token {Token}",
            email,
            userId,
            token);

        return Task.CompletedTask;
    }
}
