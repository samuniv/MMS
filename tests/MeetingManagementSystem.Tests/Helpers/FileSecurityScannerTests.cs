using MeetingManagementSystem.Core.Helpers;

namespace MeetingManagementSystem.Tests.Helpers;

public class FileSecurityScannerTests
{
    [Fact]
    public async Task ValidateFileAsync_WithOversizedFile_ReturnsInvalid()
    {
        // Arrange
        var fileSize = 11 * 1024 * 1024; // 11 MB (exceeds 10 MB limit)
        var fileName = "test.pdf";
        using var stream = new MemoryStream(new byte[fileSize]);

        // Act
        var result = await FileSecurityScanner.ValidateFileAsync(stream, fileName, fileSize);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("exceeds maximum", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateFileAsync_WithEmptyFile_ReturnsInvalid()
    {
        // Arrange
        var fileName = "test.pdf";
        using var stream = new MemoryStream();

        // Act
        var result = await FileSecurityScanner.ValidateFileAsync(stream, fileName, 0);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("empty", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateFileAsync_WithInvalidExtension_ReturnsInvalid()
    {
        // Arrange
        var fileName = "test.exe";
        var fileSize = 1024;
        using var stream = new MemoryStream(new byte[fileSize]);

        // Act
        var result = await FileSecurityScanner.ValidateFileAsync(stream, fileName, fileSize);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("not allowed", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateFileAsync_WithValidPdfFile_ReturnsValid()
    {
        // Arrange
        var fileName = "test.pdf";
        var fileSize = 1024;
        var pdfSignature = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
        var fileContent = new byte[fileSize];
        Array.Copy(pdfSignature, fileContent, pdfSignature.Length);
        using var stream = new MemoryStream(fileContent);

        // Act
        var result = await FileSecurityScanner.ValidateFileAsync(stream, fileName, fileSize);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateFileAsync_WithMismatchedSignature_ReturnsInvalid()
    {
        // Arrange
        var fileName = "test.pdf";
        var fileSize = 1024;
        var wrongSignature = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var fileContent = new byte[fileSize];
        Array.Copy(wrongSignature, fileContent, wrongSignature.Length);
        using var stream = new MemoryStream(fileContent);

        // Act
        var result = await FileSecurityScanner.ValidateFileAsync(stream, fileName, fileSize);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("does not match", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateFileAsync_WithExecutableSignature_ReturnsInvalid()
    {
        // Arrange
        var fileName = "test.pdf";
        var fileSize = 1024;
        var exeSignature = new byte[] { 0x4D, 0x5A }; // MZ (EXE)
        var fileContent = new byte[fileSize];
        Array.Copy(exeSignature, fileContent, exeSignature.Length);
        using var stream = new MemoryStream(fileContent);

        // Act
        var result = await FileSecurityScanner.ValidateFileAsync(stream, fileName, fileSize);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void GetSafeFileName_WithNormalFileName_ReturnsModifiedName()
    {
        // Arrange
        var originalFileName = "document.pdf";

        // Act
        var result = FileSecurityScanner.GetSafeFileName(originalFileName);

        // Assert
        Assert.Contains("document", result);
        Assert.EndsWith(".pdf", result);
        Assert.Contains("_", result); // Should contain timestamp and random suffix
    }

    [Fact]
    public void GetSafeFileName_WithInvalidCharacters_RemovesInvalidChars()
    {
        // Arrange
        var originalFileName = "doc<>ument?.pdf";

        // Act
        var result = FileSecurityScanner.GetSafeFileName(originalFileName);

        // Assert
        Assert.DoesNotContain("<", result);
        Assert.DoesNotContain(">", result);
        Assert.DoesNotContain("?", result);
        Assert.EndsWith(".pdf", result);
    }

    [Fact]
    public void GetSafeFileName_WithPathTraversal_RemovesPath()
    {
        // Arrange
        var originalFileName = "../../etc/passwd.txt";

        // Act
        var result = FileSecurityScanner.GetSafeFileName(originalFileName);

        // Assert
        Assert.DoesNotContain("..", result);
        Assert.DoesNotContain("/", result);
        Assert.Contains("passwd", result);
        Assert.EndsWith(".txt", result);
    }
}
