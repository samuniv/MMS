using MeetingManagementSystem.Core.Helpers;

namespace MeetingManagementSystem.Tests.Helpers;

public class InputSanitizerTests
{
    [Fact]
    public void SanitizeHtml_WithScriptTag_EncodesHtml()
    {
        // Arrange
        var input = "<script>alert('XSS')</script>";

        // Act
        var result = InputSanitizer.SanitizeHtml(input);

        // Assert
        Assert.DoesNotContain("<script>", result);
        Assert.Contains("&lt;script&gt;", result);
    }

    [Fact]
    public void RemoveDangerousCharacters_WithScriptTag_RemovesScript()
    {
        // Arrange
        var input = "Hello <script>alert('XSS')</script> World";

        // Act
        var result = InputSanitizer.RemoveDangerousCharacters(input);

        // Assert
        Assert.DoesNotContain("<script>", result);
        Assert.Contains("Hello", result);
        Assert.Contains("World", result);
    }

    [Fact]
    public void RemoveDangerousCharacters_WithEventHandler_RemovesHandler()
    {
        // Arrange
        var input = "<div onclick='alert(1)'>Click me</div>";

        // Act
        var result = InputSanitizer.RemoveDangerousCharacters(input);

        // Assert
        Assert.DoesNotContain("onclick", result);
    }

    [Fact]
    public void RemoveDangerousCharacters_WithJavascriptProtocol_RemovesProtocol()
    {
        // Arrange
        var input = "<a href='javascript:alert(1)'>Link</a>";

        // Act
        var result = InputSanitizer.RemoveDangerousCharacters(input);

        // Assert
        Assert.DoesNotContain("javascript:", result);
    }

    [Fact]
    public void SanitizeFileName_WithPathTraversal_RemovesPath()
    {
        // Arrange
        var input = "../../etc/passwd";

        // Act
        var result = InputSanitizer.SanitizeFileName(input);

        // Assert
        Assert.DoesNotContain("..", result);
        Assert.DoesNotContain("/", result);
        Assert.Equal("passwd", result);
    }

    [Fact]
    public void SanitizeFileName_WithInvalidCharacters_ReplacesWithUnderscore()
    {
        // Arrange
        var input = "file<name>with:invalid*chars?.txt";

        // Act
        var result = InputSanitizer.SanitizeFileName(input);

        // Assert
        Assert.DoesNotContain("<", result);
        Assert.DoesNotContain(">", result);
        Assert.DoesNotContain(":", result);
        Assert.DoesNotContain("*", result);
        Assert.DoesNotContain("?", result);
        Assert.Contains("_", result);
    }

    [Fact]
    public void IsValidEmail_WithValidEmail_ReturnsTrue()
    {
        // Arrange
        var email = "user@example.com";

        // Act
        var result = InputSanitizer.IsValidEmail(email);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidEmail_WithInvalidEmail_ReturnsFalse()
    {
        // Arrange
        var email = "invalid-email";

        // Act
        var result = InputSanitizer.IsValidEmail(email);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsSqlInjectionPatterns_WithSqlKeywords_ReturnsTrue()
    {
        // Arrange
        var input = "SELECT * FROM users WHERE id = 1";

        // Act
        var result = InputSanitizer.ContainsSqlInjectionPatterns(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsSqlInjectionPatterns_WithOrEquals_ReturnsTrue()
    {
        // Arrange
        var input = "' OR '1'='1";

        // Act
        var result = InputSanitizer.ContainsSqlInjectionPatterns(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsSqlInjectionPatterns_WithNormalText_ReturnsFalse()
    {
        // Arrange
        var input = "This is a normal search query";

        // Act
        var result = InputSanitizer.ContainsSqlInjectionPatterns(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void SanitizeSearchQuery_WithSpecialCharacters_RemovesSpecialChars()
    {
        // Arrange
        var input = "search<>query!@#$%";

        // Act
        var result = InputSanitizer.SanitizeSearchQuery(input);

        // Assert
        Assert.DoesNotContain("<", result);
        Assert.DoesNotContain(">", result);
        Assert.DoesNotContain("!", result);
        Assert.DoesNotContain("#", result);
        Assert.DoesNotContain("$", result);
        Assert.DoesNotContain("%", result);
        Assert.Contains("search", result);
        Assert.Contains("query", result);
    }

    [Fact]
    public void SanitizeSearchQuery_WithLongInput_TruncatesToMaxLength()
    {
        // Arrange
        var input = new string('a', 150);

        // Act
        var result = InputSanitizer.SanitizeSearchQuery(input);

        // Assert
        Assert.True(result.Length <= 100);
    }
}
