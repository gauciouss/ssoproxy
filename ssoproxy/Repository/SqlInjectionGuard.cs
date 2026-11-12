using System.Text.RegularExpressions;

namespace ssoproxy.Repository;

/// <summary>
/// SqlInjectionGuard 提供 SQL 注入防護功能，主要用於清理和驗證輸入參數，防止惡意 SQL 語句被執行。
/// </summary>
public static partial class SqlInjectionGuard
{
    private const int DefaultMaxLength = 512;
    private static readonly Regex DangerousSqlPattern = new(
        @"(?:\b(?:or|and)\s+\d+\s*=\s*\d+\b|\bunion\s+select\b|\bselect\b.*\bfrom\b|\binsert\b\s+into\b|\bupdate\b.*\bset\b|\bdelete\b\s+from\b|\bdrop\b\s+(?:table|database)\b|\balter\b\s+table\b|\btruncate\b\s+table\b|\bexec\b|\bexecute\b|\bdeclare\b|\bwaitfor\b|\bxp_\w+|\b(?:or|and)\s+1\s*=\s*1\b)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex DangerousXssPattern = new(
        @"(?i)(?:<\s*(?:script|svg|img|iframe|body|object|embed|meta|link|form|input|button|video|audio)\b|on\w+\s*=|javascript\s*:|vbscript\s*:|data\s*:\s*text/html|srcdoc\s*=)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static object? SanitizeParameter(object? value, string parameterName)
    {
        if (value is null)
        {
            return DBNull.Value;
        }

        if (value is string text)
        {
            var normalized = SanitizeText(text, parameterName);
            return normalized;
        }

        return value;
    }

    public static string SanitizeText(string? value, string parameterName, int maxLength = DefaultMaxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            trimmed = trimmed[..maxLength];
        }

        if (ContainsDangerousSqlPattern(trimmed))
        {
            throw new InvalidOperationException($"Parameter '{parameterName}' contains invalid SQL content.");
        }

        return trimmed;
    }

    public static bool IsRequestValueSuspicious(string? value, int maxLength = DefaultMaxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            return true;
        }

        return ContainsDangerousSqlPattern(normalized) || ContainsDangerousXssPattern(normalized);
    }

    public static string EscapeLikePattern(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\\", "\\\\")
            .Replace("%", "[%]")
            .Replace("_", "[_]")
            .Replace("[", "[[]");
    }

    public static bool ContainsDangerousSqlPattern(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();

        if (normalized.Any(ch => char.IsControl(ch) && ch != '\t' && ch != '\n' && ch != '\r'))
        {
            return true;
        }

        if (normalized.Contains("'--", StringComparison.Ordinal) ||
            normalized.Contains("' OR ", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("OR 1=1", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (Regex.IsMatch(normalized, @"(?i)(?:--|/\*|\*/|;)\s*(?:select|insert|update|delete|drop|alter|truncate|exec|execute|declare|waitfor|union|or\s+1\s*=\s*1|and\s+1\s*=\s*1)"))
        {
            return true;
        }

        if (Regex.IsMatch(normalized, @"(?i)(?:select|insert|update|delete|drop|alter|truncate|exec|execute|declare|waitfor|union)\b") &&
            (normalized.Contains("--", StringComparison.Ordinal) || normalized.Contains("/*", StringComparison.Ordinal) || normalized.Contains("*/", StringComparison.Ordinal) || normalized.Contains(";", StringComparison.Ordinal)))
        {
            return true;
        }

        return DangerousSqlPattern.IsMatch(normalized);
    }

    public static bool ContainsDangerousXssPattern(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();
        return DangerousXssPattern.IsMatch(normalized);
    }
}
