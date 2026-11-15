using System.Text;

namespace GroceryHub.Modules.Reporting.Application;

public static class CsvExporter
{
    private static readonly char[] FormulaPrefixes = ['=', '+', '-', '@', '\t', '\r'];

    public static string EscapeValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        string result = value;

        // Escape CSV injection / formula execution if starts with formula trigger characters
        // or if trimmed string starts with formula operators
        bool isFormula = (result.Length > 0 && FormulaPrefixes.Contains(result[0])) ||
                         (result.TrimStart().Length > 0 && FormulaPrefixes.Contains(result.TrimStart()[0]));

        if (isFormula)
        {
            result = "'" + result;
        }

        // Standard CSV quotes escaping
        if (result.Contains('"') || result.Contains(',') || result.Contains('\n') || result.Contains('\r'))
        {
            result = "\"" + result.Replace("\"", "\"\"") + "\"";
        }

        return result;
    }

    public static byte[] BuildCsv(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
    {
        var sb = new StringBuilder();

        // Write header row
        sb.AppendLine(string.Join(",", headers.Select(EscapeValue)));

        // Write data rows
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",", row.Select(EscapeValue)));
        }

        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        byte[] preamble = encoding.GetPreamble();
        byte[] contentBytes = encoding.GetBytes(sb.ToString());

        byte[] result = new byte[preamble.Length + contentBytes.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(contentBytes, 0, result, preamble.Length, contentBytes.Length);

        return result;
    }
}
