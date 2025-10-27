namespace MeetingManagementSystem.Core.Entities;

public class MeetingDocument
{
    public int Id { get; set; }
    public int MeetingId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public int UploadedById { get; set; }
    
    // Navigation Properties
    public Meeting Meeting { get; set; } = null!;
    public User UploadedBy { get; set; } = null!;
}