using System.Net;
using System.Net.Mail;
using GroceryHub.Modules.Identity.Configuration;
using GroceryHub.Modules.Identity.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GroceryHub.Modules.Identity.Application;

public sealed class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly SmtpOptions _smtpOptions;
    private readonly string _baseUrl;

    public EmailService(
        ILogger<EmailService> logger,
        IOptions<SmtpOptions> smtpOptions,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _smtpOptions = smtpOptions?.Value ?? new SmtpOptions();

        string? publicUrl = configuration["App:PublicBaseUrl"]
            ?? configuration["App__PublicBaseUrl"]
            ?? configuration["ASPNETCORE_URLS"]?.Split(';').FirstOrDefault();

        _baseUrl = !string.IsNullOrWhiteSpace(publicUrl)
            ? publicUrl.TrimEnd('/')
            : "http://localhost:5173";
    }

    public async Task SendVerificationEmailAsync(
        Guid userId,
        string email,
        string token,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        _logger.LogInformation("Preparing verification email for {Email} (UserId: {UserId})", email, userId);
        _logger.LogDebug("Verification token for UserId {UserId}: {Token}", userId, token);

        string verifyUrl = $"{_baseUrl}/verify-email?userId={userId}&token={Uri.EscapeDataString(token)}";

        string htmlBody = $$"""
            <!DOCTYPE html>
            <html lang="vi">
            <head>
              <meta charset="UTF-8">
              <style>
                body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #F8F9FA; margin: 0; padding: 24px; color: #1E293B; }
                .container { max-width: 580px; margin: 0 auto; background: #FFFFFF; border-radius: 16px; border: 1px solid #E2E8F0; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05); }
                .header { background: #16A34A; padding: 32px 24px; text-align: center; color: #FFFFFF; }
                .header h1 { margin: 0; font-size: 24px; font-weight: 700; letter-spacing: -0.5px; }
                .header p { margin: 6px 0 0; font-size: 13px; opacity: 0.9; }
                .content { padding: 32px 28px; line-height: 1.6; font-size: 14px; }
                .btn-wrapper { text-align: center; margin: 32px 0; }
                .btn { display: inline-block; background-color: #16A34A; color: #FFFFFF !important; text-decoration: none; padding: 14px 32px; border-radius: 10px; font-weight: 600; font-size: 15px; box-shadow: 0 2px 4px rgba(22, 163, 74, 0.25); }
                .token-box { background: #F1F5F9; border: 1px dashed #CBD5E1; padding: 12px; border-radius: 8px; font-family: monospace; font-size: 12px; word-break: break-all; margin: 16px 0; color: #334155; }
                .footer { background: #F8FAFC; padding: 20px 24px; text-align: center; font-size: 12px; color: #64748B; border-top: 1px solid #E2E8F0; }
              </style>
            </head>
            <body>
              <div class="container">
                <div class="header">
                  <h1>GroceryHub Vietnam</h1>
                  <p>Thực phẩm tươi sạch & Nông sản an toàn</p>
                </div>
                <div class="content">
                  <p>Xin chào quý khách,</p>
                  <p>Cảm ơn bạn đã đăng ký tài khoản tại siêu thị thực phẩm tươi sạch <strong>GroceryHub</strong>. Vui lòng bấm vào nút bên dưới để hoàn tất việc xác thực địa chỉ email và kích hoạt tài khoản của bạn:</p>
                  
                  <div class="btn-wrapper">
                    <a href="{{verifyUrl}}" class="btn" target="_blank">Xác Thực Tài Khoản Ngay</a>
                  </div>

                  <p style="font-size: 13px; color: #64748B;">Nếu nút bấm không hoạt động, bạn có thể sao chép liên kết sau vào trình duyệt:</p>
                  <div class="token-box">{{verifyUrl}}</div>

                  <p style="font-size: 12px; color: #94A3B8; margin-top: 24px;">* Liên kết xác thực có hiệu lực trong vòng 24 giờ. Nếu bạn không tạo tài khoản này, vui lòng bỏ qua email.</p>
                </div>
                <div class="footer">
                  <p>© 2026 GroceryHub Vietnam. Địa chỉ: Thôn 9, Xã Suối Hai, Huyện Ba Vì, Hà Nội.<br>Hotline hỗ trợ: 0865 087 784</p>
                </div>
              </div>
            </body>
            </html>
            """;

        await SendEmailInternalAsync(email, "Xác thực tài khoản GroceryHub Vietnam", htmlBody, ct);
    }

    public async Task SendPasswordResetEmailAsync(
        Guid userId,
        string email,
        string token,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        _logger.LogInformation("Preparing password reset email for {Email} (UserId: {UserId})", email, userId);
        _logger.LogDebug("Password reset token for UserId {UserId}: {Token}", userId, token);

        string resetUrl = $"{_baseUrl}/reset-password?userId={userId}&token={Uri.EscapeDataString(token)}";

        string htmlBody = $$"""
            <!DOCTYPE html>
            <html lang="vi">
            <head>
              <meta charset="UTF-8">
              <style>
                body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #F8F9FA; margin: 0; padding: 24px; color: #1E293B; }
                .container { max-width: 580px; margin: 0 auto; background: #FFFFFF; border-radius: 16px; border: 1px solid #E2E8F0; overflow: hidden; }
                .header { background: #16A34A; padding: 32px 24px; text-align: center; color: #FFFFFF; }
                .header h1 { margin: 0; font-size: 24px; font-weight: 700; }
                .content { padding: 32px 28px; line-height: 1.6; font-size: 14px; }
                .btn-wrapper { text-align: center; margin: 32px 0; }
                .btn { display: inline-block; background-color: #16A34A; color: #FFFFFF !important; text-decoration: none; padding: 14px 32px; border-radius: 10px; font-weight: 600; font-size: 15px; }
                .token-box { background: #F1F5F9; border: 1px dashed #CBD5E1; padding: 12px; border-radius: 8px; font-family: monospace; font-size: 12px; word-break: break-all; margin: 16px 0; }
                .footer { background: #F8FAFC; padding: 20px 24px; text-align: center; font-size: 12px; color: #64748B; border-top: 1px solid #E2E8F0; }
              </style>
            </head>
            <body>
              <div class="container">
                <div class="header">
                  <h1>GroceryHub Vietnam</h1>
                  <p>Yêu cầu đặt lại mật khẩu</p>
                </div>
                <div class="content">
                  <p>Xin chào quý khách,</p>
                  <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản <strong>{{email}}</strong> tại GroceryHub. Vui lòng bấm vào nút bên dưới để tiến hành đổi mật khẩu mới:</p>
                  
                  <div class="btn-wrapper">
                    <a href="{{resetUrl}}" class="btn" target="_blank">Đặt Lại Mật Khẩu</a>
                  </div>

                  <p style="font-size: 13px; color: #64748B;">Hoặc sao chép liên kết sau vào trình duyệt:</p>
                  <div class="token-box">{{resetUrl}}</div>

                  <p style="font-size: 12px; color: #94A3B8; margin-top: 24px;">* Liên kết này có giá trị trong vòng 24 giờ. Nếu bạn không yêu cầu, vui lòng đổi mật khẩu tài khoản ngay để bảo mật.</p>
                </div>
                <div class="footer">
                  <p>© 2026 GroceryHub Vietnam. Hotline: 0865 087 784</p>
                </div>
              </div>
            </body>
            </html>
            """;

        await SendEmailInternalAsync(email, "Yêu cầu đặt lại mật khẩu GroceryHub Vietnam", htmlBody, ct);
    }

    private async Task SendEmailInternalAsync(
        string recipientEmail,
        string subject,
        string htmlBody,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_smtpOptions.Host))
        {
            _logger.LogWarning("SMTP Host is not configured. Email to {Email} was skipped.", recipientEmail);
            return;
        }

        try
        {
            using var client = new SmtpClient(_smtpOptions.Host, _smtpOptions.Port)
            {
                EnableSsl = _smtpOptions.Port == 587 || _smtpOptions.Port == 465 || _smtpOptions.EnableSsl,
                Timeout = 15000,
            };

            if (!string.IsNullOrWhiteSpace(_smtpOptions.User) && !string.IsNullOrWhiteSpace(_smtpOptions.Password))
            {
                client.Credentials = new NetworkCredential(_smtpOptions.User, _smtpOptions.Password);
            }

            string fromAddress = !string.IsNullOrWhiteSpace(_smtpOptions.From)
                ? _smtpOptions.From
                : "no-reply@grocery.dangbinhduong.io.vn";

            using var message = new MailMessage
            {
                From = new MailAddress(fromAddress, "GroceryHub Vietnam"),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
            };

            message.To.Add(recipientEmail);

            await client.SendMailAsync(message, ct);
            _logger.LogInformation("Email successfully dispatched via SMTP ({Host}:{Port}) to {Email}", _smtpOptions.Host, _smtpOptions.Port, recipientEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} via SMTP ({Host}:{Port})", recipientEmail, _smtpOptions.Host, _smtpOptions.Port);
        }
    }
}
