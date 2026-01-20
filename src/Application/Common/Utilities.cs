using System.Text;
using System.Text.RegularExpressions;

namespace Application.Common;

public static class Utilities
{
    public static string EscapeLike(string s) => 
        s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
    
    public static string Slugify(this string value)
    {
        var normalized = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        var cleaned = Regex.Replace(sb.ToString(), @"[^a-z0-9]+", "-");
        cleaned = Regex.Replace(cleaned, @"-+", "-").Trim('-');
        return cleaned;
    }

    // TODO: Move to Database Sequence.
    public static string GenerateSku(string categoryCode) =>
        $"{categoryCode}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
}