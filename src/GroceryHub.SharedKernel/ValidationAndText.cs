using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace GroceryHub.SharedKernel;

public static class Guard
{
    public static T AgainstNull<T>(
        T? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : class
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }

        return value;
    }

    public static string AgainstNullOrWhiteSpace(
        string? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", paramName);
        }

        return value.Trim();
    }

    public static string AgainstExceedingLength(
        string value,
        int maxLength,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        AgainstNull(value, paramName);
        if (value.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, value.Length, $"Length cannot exceed {maxLength} characters.");
        }

        return value;
    }

    public static int AgainstOutOfRange(
        int value,
        int minInclusive,
        int maxInclusive,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value < minInclusive || value > maxInclusive)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"Value must be between {minInclusive} and {maxInclusive}.");
        }

        return value;
    }

    public static long AgainstNegative(
        long value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Value cannot be negative.");
        }

        return value;
    }

    public static Guid AgainstEmptyGuid(
        Guid value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier cannot be Guid.Empty.", paramName);
        }

        return value;
    }
}

public static partial class VietnamesePhone
{
    [GeneratedRegex(@"^0[35789]\d{8}$", RegexOptions.CultureInvariant)]
    private static partial Regex NormalizedPhoneRegex();

    public static string Normalize(string rawPhone)
    {
        if (string.IsNullOrWhiteSpace(rawPhone))
        {
            return string.Empty;
        }

        StringBuilder digits = new(rawPhone.Length);
        string trimmed = rawPhone.Trim();
        bool hasLeadingPlus = trimmed.StartsWith('+');

        foreach (char c in trimmed)
        {
            if (char.IsAsciiDigit(c))
            {
                digits.Append(c);
            }
        }

        string candidate = digits.ToString();
        if (hasLeadingPlus && candidate.StartsWith("84", StringComparison.Ordinal) && candidate.Length == 11)
        {
            return string.Concat("0", candidate.AsSpan(2));
        }

        if (!hasLeadingPlus && candidate.StartsWith("84", StringComparison.Ordinal) && candidate.Length == 11)
        {
            return string.Concat("0", candidate.AsSpan(2));
        }

        return candidate;
    }

    public static bool IsValid(string? rawPhone)
    {
        if (string.IsNullOrWhiteSpace(rawPhone))
        {
            return false;
        }

        string normalized = Normalize(rawPhone);
        return NormalizedPhoneRegex().IsMatch(normalized);
    }

    public static Result<string> TryNormalize(string? rawPhone)
    {
        string normalized = Normalize(rawPhone ?? string.Empty);
        if (!NormalizedPhoneRegex().IsMatch(normalized))
        {
            return DomainError.Validation(
                ErrorCodes.ValidationFailed,
                "Phone number must be a valid 10-digit Vietnamese mobile number starting with 03, 05, 07, 08, or 09.",
                new Dictionary<string, string[]>
                {
                    ["phone"] = ["Invalid Vietnamese mobile phone number."]
                });
        }

        return normalized;
    }
}

public static class VietnameseTextNormalizer
{
    public static string NormalizeForSearch(params string?[] segments)
    {
        if (segments is null || segments.Length == 0)
        {
            return string.Empty;
        }

        string combined = string.Join(' ', segments.Where(static s => !string.IsNullOrWhiteSpace(s)));
        return RemoveDiacritics(combined);
    }

    public static string RemoveDiacritics(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        string decomposed = input.Normalize(NormalizationForm.FormD);
        StringBuilder sb = new(decomposed.Length);
        bool previousWasSpace = true;

        foreach (char ch in decomposed)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            char mapped = ch switch
            {
                'đ' or 'Đ' => 'd',
                _ => char.ToLowerInvariant(ch)
            };

            if (char.IsLetterOrDigit(mapped))
            {
                sb.Append(mapped);
                previousWasSpace = false;
            }
            else if (!previousWasSpace)
            {
                sb.Append(' ');
                previousWasSpace = true;
            }
        }

        if (sb.Length > 0 && sb[^1] == ' ')
        {
            sb.Length--;
        }

        return sb.ToString();
    }

    public static string ToSlug(string? input)
    {
        string normalized = RemoveDiacritics(input);
        return normalized.Replace(' ', '-');
    }
}
