using System.Text.RegularExpressions;
using System.Web;

namespace MeetingManagementSystem.Core.Helpers;

public static class InputSanitizer
{
    /// <summary>
    /// Sanitizes HTML input by encoding special characters
    /// </summary>
    public static string SanitizeHtml(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return HttpUtility.HtmlEncode(input);
    }

    /// <summary>
    /// Removes potentially dangerous characters from input
    /// </summary>
    public static string RemoveDangerousCharacters(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Remove script tags and their content
        input = Regex.Replace(input, @"<script[^>]*>.*?</script>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        
        // Remove potentially dangerous HTML tags
        input = Regex.Replace(input, @"<(iframe|object|embed|link|meta|style)[^>]*>.*?</\1>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        
        // Remove event handlers
        input = Regex.Replace(input, @"\s*on\w+\s*=\s*[""'][^""']*[""']", string.Empty, RegexOptions.IgnoreCase);
        
        // Remove javascript: protocol
        input = Regex.Replace(input, @"javascript:", string.Empty, RegexOptions.IgnoreCase);
        
        return input;
    }

    /// <summary>
    /// Validates and sanitizes file names
    /// </summary>
    public static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return fileName;

        // Remove path traversal attempts
        fileName = Path.GetFileName(fileName);
        
        // Remove invalid file name characters
        var invalidChars = Path.GetInvalidFileNameChars();
        fileName = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        
        // Limit length
        if (fileName.Length > 255)
            fileName = fileName.Substring(0, 255);
        
        return fileName;
    }

    /// <summary>
    /// Validates email format
    /// </summary>
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, emailPattern, RegexOptions.IgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates that input doesn't contain SQL injection patterns
    /// </summary>
    public static bool ContainsSqlInjectionPatterns(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        var sqlPatterns = new[]
        {
            @"(\b(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|EXEC|EXECUTE)\b)",
            @"(--|\;|\/\*|\*\/)",
            @"(\bOR\b.*=.*)",
            @"(\bAND\b.*=.*)",
            @"('|"")(\s)*(OR|AND)(\s)*('|"")",
        };

        return sqlPatterns.Any(pattern => 
            Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// Sanitizes search query input
    /// </summary>
    public static string SanitizeSearchQuery(string query)
    {
        if (string.IsNullOrEmpty(query))
            return query;

        // Remove special characters that could be used for injection
        query = Regex.Replace(query, @"[^\w\s\-@.]", string.Empty);
        
        // Limit length
        if (query.Length > 100)
            query = query.Substring(0, 100);
        
        return query.Trim();
    }
}
