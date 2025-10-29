using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Interfaces;

namespace MeetingManagementSystem.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<NotificationService> _logger;
    private readonly INotificationPreferenceService _preferenceService;

    public NotificationService(
        IOptions<EmailSettings> emailSettings,
        ILogger<NotificationService> logger,
        INotificationPreferenceService preferenceService)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
        _preferenceService = preferenceService;
    }

    public async Task SendMeetingInvitationAsync(Meeting meeting, IEnumerable<User> participants)
    {
        _logger.LogInformation("Sending meeting invitation for meeting {MeetingId} to {ParticipantCount} participants",
            meeting.Id, participants.Count());

        var subject = $"Meeting Invitation: {meeting.Title}";
        var body = BuildMeetingInvitationTemplate(meeting);

        foreach (var participant in participants)
        {
            if (!string.IsNullOrEmpty(participant.Email))
            {
                // Check if user wants to receive meeting invitations
                if (await _preferenceService.ShouldSendNotificationAsync(participant.Id, "MeetingInvitation"))
                {
                    await SendEmailWithRetryAsync(participant.Email, subject, body);
                    
                    // Log notification history
                    await _preferenceService.LogNotificationAsync(new NotificationHistory
                    {
                        UserId = participant.Id,
                        NotificationType = "MeetingInvitation",
                        Subject = subject,
                        Message = $"Invitation to meeting: {meeting.Title}",
                        DeliveryMethod = "Email",
                        IsDelivered = true,
                        RelatedMeetingId = meeting.Id,
                        SentAt = DateTime.UtcNow,
                        DeliveredAt = DateTime.UtcNow
                    });
                }
                else
                {
                    _logger.LogInformation("Skipping meeting invitation for user {UserId} due to notification preferences", participant.Id);
                }
            }
        }
    }

    public async Task SendMeetingReminderAsync(Meeting meeting, TimeSpan reminderTime)
    {
        _logger.LogInformation("Sending meeting reminder for meeting {MeetingId} ({ReminderTime} before)",
            meeting.Id, reminderTime);

        var subject = $"Meeting Reminder: {meeting.Title}";
        var body = BuildMeetingReminderTemplate(meeting, reminderTime);

        foreach (var participant in meeting.Participants)
        {
            if (!string.IsNullOrEmpty(participant.User.Email))
            {
                if (await _preferenceService.ShouldSendNotificationAsync(participant.UserId, "MeetingReminder"))
                {
                    await SendEmailWithRetryAsync(participant.User.Email, subject, body);
                    
                    await _preferenceService.LogNotificationAsync(new NotificationHistory
                    {
                        UserId = participant.UserId,
                        NotificationType = "MeetingReminder",
                        Subject = subject,
                        Message = $"Reminder for meeting: {meeting.Title}",
                        DeliveryMethod = "Email",
                        IsDelivered = true,
                        RelatedMeetingId = meeting.Id,
                        SentAt = DateTime.UtcNow,
                        DeliveredAt = DateTime.UtcNow
                    });
                }
            }
        }

        // Also send to organizer
        if (!string.IsNullOrEmpty(meeting.Organizer.Email))
        {
            if (await _preferenceService.ShouldSendNotificationAsync(meeting.OrganizerId, "MeetingReminder"))
            {
                await SendEmailWithRetryAsync(meeting.Organizer.Email, subject, body);
                
                await _preferenceService.LogNotificationAsync(new NotificationHistory
                {
                    UserId = meeting.OrganizerId,
                    NotificationType = "MeetingReminder",
                    Subject = subject,
                    Message = $"Reminder for meeting: {meeting.Title}",
                    DeliveryMethod = "Email",
                    IsDelivered = true,
                    RelatedMeetingId = meeting.Id,
                    SentAt = DateTime.UtcNow,
                    DeliveredAt = DateTime.UtcNow
                });
            }
        }
    }

    public async Task SendMeetingCancellationAsync(Meeting meeting, string reason)
    {
        _logger.LogInformation("Sending meeting cancellation for meeting {MeetingId}", meeting.Id);

        var subject = $"Meeting Cancelled: {meeting.Title}";
        var body = BuildMeetingCancellationTemplate(meeting, reason);

        foreach (var participant in meeting.Participants)
        {
            if (!string.IsNullOrEmpty(participant.User.Email))
            {
                await SendEmailWithRetryAsync(participant.User.Email, subject, body);
            }
        }
    }

    public async Task SendActionItemReminderAsync(ActionItem actionItem)
    {
        _logger.LogInformation("Sending action item reminder for action item {ActionItemId}", actionItem.Id);

        if (string.IsNullOrEmpty(actionItem.AssignedTo.Email))
        {
            _logger.LogWarning("Cannot send action item reminder - user {UserId} has no email", 
                actionItem.AssignedToId);
            return;
        }

        if (await _preferenceService.ShouldSendNotificationAsync(actionItem.AssignedToId, "ActionItemReminder"))
        {
            var subject = $"Action Item Reminder: Due {actionItem.DueDate:yyyy-MM-dd}";
            var body = BuildActionItemReminderTemplate(actionItem);

            await SendEmailWithRetryAsync(actionItem.AssignedTo.Email, subject, body);
            
            await _preferenceService.LogNotificationAsync(new NotificationHistory
            {
                UserId = actionItem.AssignedToId,
                NotificationType = "ActionItemReminder",
                Subject = subject,
                Message = $"Reminder for action item: {actionItem.Description}",
                DeliveryMethod = "Email",
                IsDelivered = true,
                RelatedActionItemId = actionItem.Id,
                SentAt = DateTime.UtcNow,
                DeliveredAt = DateTime.UtcNow
            });
        }
    }

    public async Task SendActionItemAssignmentAsync(ActionItem actionItem)
    {
        _logger.LogInformation("Sending action item assignment notification for action item {ActionItemId}", actionItem.Id);

        if (string.IsNullOrEmpty(actionItem.AssignedTo.Email))
        {
            _logger.LogWarning("Cannot send action item assignment - user {UserId} has no email", 
                actionItem.AssignedToId);
            return;
        }

        var subject = $"New Action Item Assigned: {actionItem.Description}";
        var body = BuildActionItemAssignmentTemplate(actionItem);

        await SendEmailWithRetryAsync(actionItem.AssignedTo.Email, subject, body);
    }

    public async Task SendMeetingUpdateNotificationAsync(Meeting meeting, string updateMessage)
    {
        _logger.LogInformation("Sending meeting update notification for meeting {MeetingId}", meeting.Id);

        var subject = $"Meeting Update: {meeting.Title}";
        var body = BuildMeetingUpdateTemplate(meeting, updateMessage);

        foreach (var participant in meeting.Participants)
        {
            if (!string.IsNullOrEmpty(participant.User.Email))
            {
                await SendEmailWithRetryAsync(participant.User.Email, subject, body);
            }
        }
    }

    public async Task SendAttendanceConfirmationAsync(Meeting meeting, User participant, bool isAccepted)
    {
        _logger.LogInformation("Sending attendance confirmation to organizer for meeting {MeetingId}", meeting.Id);

        if (string.IsNullOrEmpty(meeting.Organizer.Email))
        {
            _logger.LogWarning("Cannot send attendance confirmation - organizer has no email");
            return;
        }

        var status = isAccepted ? "accepted" : "declined";
        var subject = $"Attendance {status}: {meeting.Title}";
        var body = BuildAttendanceConfirmationTemplate(meeting, participant, isAccepted);

        await SendEmailWithRetryAsync(meeting.Organizer.Email, subject, body);
    }

    private async Task SendEmailWithRetryAsync(string toEmail, string subject, string body)
    {
        var attempt = 0;
        var maxAttempts = _emailSettings.MaxRetryAttempts;

        while (attempt < maxAttempts)
        {
            try
            {
                await SendEmailAsync(toEmail, subject, body);
                _logger.LogInformation("Email sent successfully to {Email} on attempt {Attempt}", 
                    toEmail, attempt + 1);
                return;
            }
            catch (Exception ex)
            {
                attempt++;
                _logger.LogWarning(ex, "Failed to send email to {Email} on attempt {Attempt}/{MaxAttempts}",
                    toEmail, attempt, maxAttempts);

                if (attempt < maxAttempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(_emailSettings.RetryDelaySeconds * attempt));
                }
                else
                {
                    _logger.LogError(ex, "Failed to send email to {Email} after {MaxAttempts} attempts",
                        toEmail, maxAttempts);
                    throw;
                }
            }
        }
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        using var smtpClient = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
        {
            EnableSsl = _emailSettings.EnableSsl
        };

        if (!string.IsNullOrEmpty(_emailSettings.Username) && !string.IsNullOrEmpty(_emailSettings.Password))
        {
            smtpClient.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);
        }

        var mailMessage = new MailMessage
        {
            From = new MailAddress(_emailSettings.FromAddress, _emailSettings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        mailMessage.To.Add(toEmail);

        await smtpClient.SendMailAsync(mailMessage);
    }

    private string BuildMeetingInvitationTemplate(Meeting meeting)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<html><body>");
        sb.AppendLine("<h2>Meeting Invitation</h2>");
        sb.AppendLine($"<p>You have been invited to the following meeting:</p>");
        sb.AppendLine($"<h3>{meeting.Title}</h3>");
        sb.AppendLine($"<p><strong>Description:</strong> {meeting.Description}</p>");
        sb.AppendLine($"<p><strong>Date:</strong> {meeting.ScheduledDate:dddd, MMMM dd, yyyy}</p>");
        sb.AppendLine($"<p><strong>Time:</strong> {meeting.StartTime:hh\\:mm} - {meeting.EndTime:hh\\:mm}</p>");
        
        if (meeting.MeetingRoom != null)
        {
            sb.AppendLine($"<p><strong>Location:</strong> {meeting.MeetingRoom.Name} ({meeting.MeetingRoom.Location})</p>");
        }
        
        sb.AppendLine($"<p><strong>Organizer:</strong> {meeting.Organizer.FirstName} {meeting.Organizer.LastName}</p>");
        sb.AppendLine("<p>Please confirm your attendance at your earliest convenience.</p>");
        sb.AppendLine("</body></html>");
        
        return sb.ToString();
    }

    private string BuildMeetingReminderTemplate(Meeting meeting, TimeSpan reminderTime)
    {
        var sb = new StringBuilder();
        var reminderText = reminderTime.TotalHours >= 24 
            ? $"{reminderTime.TotalHours / 24:F0} day(s)" 
            : $"{reminderTime.TotalHours:F0} hour(s)";

        sb.AppendLine("<html><body>");
        sb.AppendLine("<h2>Meeting Reminder</h2>");
        sb.AppendLine($"<p>This is a reminder that you have a meeting in {reminderText}:</p>");
        sb.AppendLine($"<h3>{meeting.Title}</h3>");
        sb.AppendLine($"<p><strong>Date:</strong> {meeting.ScheduledDate:dddd, MMMM dd, yyyy}</p>");
        sb.AppendLine($"<p><strong>Time:</strong> {meeting.StartTime:hh\\:mm} - {meeting.EndTime:hh\\:mm}</p>");
        
        if (meeting.MeetingRoom != null)
        {
            sb.AppendLine($"<p><strong>Location:</strong> {meeting.MeetingRoom.Name} ({meeting.MeetingRoom.Location})</p>");
        }
        
        sb.AppendLine("</body></html>");
        
        return sb.ToString();
    }

    private string BuildMeetingCancellationTemplate(Meeting meeting, string reason)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<html><body>");
        sb.AppendLine("<h2>Meeting Cancelled</h2>");
        sb.AppendLine($"<p>The following meeting has been cancelled:</p>");
        sb.AppendLine($"<h3>{meeting.Title}</h3>");
        sb.AppendLine($"<p><strong>Originally Scheduled:</strong> {meeting.ScheduledDate:dddd, MMMM dd, yyyy} at {meeting.StartTime:hh\\:mm}</p>");
        sb.AppendLine($"<p><strong>Reason:</strong> {reason}</p>");
        sb.AppendLine($"<p><strong>Organizer:</strong> {meeting.Organizer.FirstName} {meeting.Organizer.LastName}</p>");
        sb.AppendLine("</body></html>");
        
        return sb.ToString();
    }

    private string BuildActionItemReminderTemplate(ActionItem actionItem)
    {
        var sb = new StringBuilder();
        var daysUntilDue = (actionItem.DueDate - DateTime.UtcNow).Days;
        
        sb.AppendLine("<html><body>");
        sb.AppendLine("<h2>Action Item Reminder</h2>");
        sb.AppendLine($"<p>You have an action item due in {daysUntilDue} day(s):</p>");
        sb.AppendLine($"<p><strong>Description:</strong> {actionItem.Description}</p>");
        sb.AppendLine($"<p><strong>Due Date:</strong> {actionItem.DueDate:dddd, MMMM dd, yyyy}</p>");
        sb.AppendLine($"<p><strong>Status:</strong> {actionItem.Status}</p>");
        sb.AppendLine($"<p><strong>Meeting:</strong> {actionItem.AgendaItem.Meeting.Title}</p>");
        sb.AppendLine("<p>Please ensure this action item is completed by the due date.</p>");
        sb.AppendLine("</body></html>");
        
        return sb.ToString();
    }

    private string BuildActionItemAssignmentTemplate(ActionItem actionItem)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("<html><body>");
        sb.AppendLine("<h2>New Action Item Assigned</h2>");
        sb.AppendLine($"<p>You have been assigned a new action item:</p>");
        sb.AppendLine($"<p><strong>Description:</strong> {actionItem.Description}</p>");
        sb.AppendLine($"<p><strong>Due Date:</strong> {actionItem.DueDate:dddd, MMMM dd, yyyy}</p>");
        sb.AppendLine($"<p><strong>Meeting:</strong> {actionItem.AgendaItem.Meeting.Title}</p>");
        sb.AppendLine($"<p><strong>Agenda Item:</strong> {actionItem.AgendaItem.Title}</p>");
        sb.AppendLine("<p>Please review and complete this action item by the due date.</p>");
        sb.AppendLine("</body></html>");
        
        return sb.ToString();
    }

    private string BuildMeetingUpdateTemplate(Meeting meeting, string updateMessage)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<html><body>");
        sb.AppendLine("<h2>Meeting Update</h2>");
        sb.AppendLine($"<p>The following meeting has been updated:</p>");
        sb.AppendLine($"<h3>{meeting.Title}</h3>");
        sb.AppendLine($"<p><strong>Update:</strong> {updateMessage}</p>");
        sb.AppendLine($"<p><strong>Date:</strong> {meeting.ScheduledDate:dddd, MMMM dd, yyyy}</p>");
        sb.AppendLine($"<p><strong>Time:</strong> {meeting.StartTime:hh\\:mm} - {meeting.EndTime:hh\\:mm}</p>");
        sb.AppendLine("</body></html>");
        
        return sb.ToString();
    }

    private string BuildAttendanceConfirmationTemplate(Meeting meeting, User participant, bool isAccepted)
    {
        var sb = new StringBuilder();
        var status = isAccepted ? "accepted" : "declined";
        
        sb.AppendLine("<html><body>");
        sb.AppendLine("<h2>Attendance Confirmation</h2>");
        sb.AppendLine($"<p>{participant.FirstName} {participant.LastName} has {status} the meeting invitation:</p>");
        sb.AppendLine($"<h3>{meeting.Title}</h3>");
        sb.AppendLine($"<p><strong>Date:</strong> {meeting.ScheduledDate:dddd, MMMM dd, yyyy}</p>");
        sb.AppendLine($"<p><strong>Time:</strong> {meeting.StartTime:hh\\:mm} - {meeting.EndTime:hh\\:mm}</p>");
        sb.AppendLine("</body></html>");
        
        return sb.ToString();
    }
}
