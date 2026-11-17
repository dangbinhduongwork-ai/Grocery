namespace GroceryHub.Modules.Identity.Configuration;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string? Host { get; set; } = "mailpit";
    public int Port { get; set; } = 1025;
    public string? User { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "no-reply@grocery.dangbinhduong.io.vn";
    public bool EnableSsl { get; set; } = false;
}
