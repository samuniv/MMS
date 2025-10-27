namespace MeetingManagementSystem.Core.DTOs;

public class EmailSettings
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public bool EnableSsl { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Meeting Management System";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
}
