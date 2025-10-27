namespace MeetingManagementSystem.Core.DTOs;

public class DocumentSettings
{
    public int MaxDocumentSizeMB { get; set; } = 10;
    public string[] AllowedFileTypes { get; set; } = { ".pdf", ".doc", ".docx", ".xls", ".xlsx" };
    public string UploadPath { get; set; } = "uploads";
}
