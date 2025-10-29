namespace MeetingManagementSystem.Core.Helpers;

public static class FileSecurityScanner
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".csv"
    };

    private static readonly Dictionary<string, byte[]> FileSignatures = new()
    {
        { ".pdf", new byte[] { 0x25, 0x50, 0x44, 0x46 } }, // %PDF
        { ".doc", new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } }, // DOC
        { ".docx", new byte[] { 0x50, 0x4B, 0x03, 0x04 } }, // ZIP (DOCX is ZIP)
        { ".xls", new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } }, // XLS
        { ".xlsx", new byte[] { 0x50, 0x4B, 0x03, 0x04 } }, // ZIP (XLSX is ZIP)
        { ".ppt", new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } }, // PPT
        { ".pptx", new byte[] { 0x50, 0x4B, 0x03, 0x04 } }, // ZIP (PPTX is ZIP)
        { ".txt", new byte[] { } }, // Text files don't have a specific signature
        { ".csv", new byte[] { } }  // CSV files don't have a specific signature
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Validates file upload for security concerns
    /// </summary>
    public static async Task<FileValidationResult> ValidateFileAsync(Stream fileStream, string fileName, long fileSize)
    {
        var result = new FileValidationResult { IsValid = true };

        // Check if stream is null
        if (fileStream == null)
        {
            result.IsValid = false;
            result.ErrorMessage = "File stream is null";
            return result;
        }

        // Check file size
        if (fileSize > MaxFileSizeBytes)
        {
            result.IsValid = false;
            result.ErrorMessage = $"File size exceeds maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)} MB";
            return result;
        }

        if (fileSize == 0)
        {
            result.IsValid = false;
            result.ErrorMessage = "File is empty";
            return result;
        }

        // Check file extension
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            result.IsValid = false;
            result.ErrorMessage = $"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}";
            return result;
        }

        // Validate file signature (magic bytes)
        if (FileSignatures.TryGetValue(extension, out var expectedSignature) && expectedSignature != null && expectedSignature.Length > 0)
        {
            var actualSignature = new byte[expectedSignature.Length];
            fileStream.Position = 0;
            var bytesRead = await fileStream.ReadAsync(actualSignature, 0, expectedSignature.Length);
            fileStream.Position = 0;

            if (bytesRead < expectedSignature.Length || !actualSignature.SequenceEqual(expectedSignature))
            {
                result.IsValid = false;
                result.ErrorMessage = "File content does not match its extension. Possible file type mismatch or corruption.";
                return result;
            }
        }

        // Check for embedded executables or scripts
        if (await ContainsSuspiciousContentAsync(fileStream))
        {
            result.IsValid = false;
            result.ErrorMessage = "File contains suspicious content and cannot be uploaded";
            return result;
        }

        return result;
    }

    /// <summary>
    /// Scans file content for suspicious patterns
    /// </summary>
    private static async Task<bool> ContainsSuspiciousContentAsync(Stream fileStream)
    {
        fileStream.Position = 0;
        var buffer = new byte[8192];
        var bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length);
        fileStream.Position = 0;

        if (bytesRead == 0)
            return false;

        // Check for executable signatures
        var suspiciousPatterns = new List<byte[]>
        {
            new byte[] { 0x4D, 0x5A }, // MZ (EXE)
            new byte[] { 0x7F, 0x45, 0x4C, 0x46 }, // ELF
            new byte[] { 0xCA, 0xFE, 0xBA, 0xBE }, // Mach-O
        };

        foreach (var pattern in suspiciousPatterns)
        {
            if (ContainsPattern(buffer, pattern, bytesRead))
                return true;
        }

        return false;
    }

    private static bool ContainsPattern(byte[] buffer, byte[] pattern, int bufferLength)
    {
        for (int i = 0; i <= bufferLength - pattern.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (buffer[i + j] != pattern[j])
                {
                    match = false;
                    break;
                }
            }
            if (match)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Gets safe file name for storage
    /// </summary>
    public static string GetSafeFileName(string originalFileName)
    {
        var fileName = Path.GetFileNameWithoutExtension(originalFileName);
        var extension = Path.GetExtension(originalFileName);

        // Remove invalid characters
        fileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));

        // Add timestamp to prevent collisions
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var randomSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);

        return $"{fileName}_{timestamp}_{randomSuffix}{extension}";
    }
}
