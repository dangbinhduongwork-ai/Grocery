using GroceryHub.Modules.Payments.Configuration;
using Microsoft.Extensions.Options;

namespace GroceryHub.Modules.Payments.Application;

public interface IVietQrUrlBuilder
{
    string BuildQrUrl(string accountNumber, string bankShortName, long amount, string transferContent);
}

public sealed class VietQrUrlBuilder : IVietQrUrlBuilder
{
    private readonly SePayOptions _options;

    public VietQrUrlBuilder(IOptions<SePayOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    public string BuildQrUrl(string accountNumber, string bankShortName, long amount, string transferContent)
    {
        string baseUrl = string.IsNullOrWhiteSpace(_options.QrBaseUrl)
            ? "https://qr.sepay.vn/img"
            : _options.QrBaseUrl.TrimEnd('/');

        string acc = Uri.EscapeDataString(accountNumber);
        string bank = Uri.EscapeDataString(bankShortName);
        string des = Uri.EscapeDataString(transferContent);

        return $"{baseUrl}?acc={acc}&bank={bank}&amount={amount}&des={des}&template=compact&download=false";
    }
}
