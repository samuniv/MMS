using Microsoft.Extensions.Logging;
using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Enums;
using MeetingManagementSystem.Core.Exceptions;
using MeetingManagementSystem.Core.Interfaces;

namespace MeetingManagementSystem.Infrastructure.Services;

public class MeetingService : IMeetingService
{
    private readonly IMeetingRepository _meetingRepository;
    private readonly IMeetingParticipantRepository _participantRepository;
    private readonly IMeetingRoomRepository _roomRepository;
    private readonly IReminderSchedulerService _reminderScheduler;
    private readonly INotificationService _notificationService;
    private readonly ILogger<MeetingService> _logger;

    public MeetingService(
        IMeetingRepository meetingRepository,
        IMeetingParticipantRepository participantRepository,
        IMeetingRoomRepository roomRepository,
        IReminderSchedulerService reminderScheduler,
        INotificationService notificationService,
        ILogger<MeetingService> logger)
    {
        _meetingRepository = meetingRepository;
        _participantRepository = participantRepository;
        _roomRepository = roomRepository;
        _reminderScheduler = reminderScheduler;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Meeting> CreateMeetingAsync(CreateMeetingDto dto)
    {
        _logger.LogInformation("Creating meeting: {Title}", dto.Title);

        // Check for room conflicts if room is specified
        if (dto.MeetingRoomId.HasValue)
        {
            var hasConflict = await _meetingRepository.HasRoomConflictAsync(
                dto.MeetingRoomId, dto.ScheduledDate, dto.StartTime, dto.EndTime);

            if (hasConflict)
            {
                _logger.LogWarning("Room conflict detected for room {RoomId}", dto.MeetingRoomId);
                throw new RoomNotAvailableException(dto.MeetingRoomId.Value, dto.ScheduledDate, dto.StartTime);
            }
        }

        var meeting = new Meeting
        {
            Title = dto.Title,
            Description = dto.Description,
            ScheduledDate = dto.ScheduledDate,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            OrganizerId = dto.OrganizerId,
            MeetingRoomId = dto.MeetingRoomId,
            Status = MeetingStatus.Scheduled,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdMeeting = await _meetingRepository.AddAsync(meeting);

        // Add participants
        if (dto.ParticipantIds.Any())
        {
            var participants = dto.ParticipantIds.Select(userId => new MeetingParticipant
            {
                MeetingId = createdMeeting.Id,
                UserId = userId,
                AttendanceStatus = AttendanceStatus.Pending
            });

            await _participantRepository.AddRangeAsync(participants);
        }

        // Schedule reminders for the meeting
        await _reminderScheduler.ScheduleMeetingRemindersAsync(createdMeeting.Id);

        // Send invitations to participants
        var meetingWithDetails = await _meetingRepository.GetMeetingWithDetailsAsync(createdMeeting.Id);
        if (meetingWithDetails != null && meetingWithDetails.Participants.Any())
        {
            var participantUsers = meetingWithDetails.Participants.Select(p => p.User);
            await _notificationService.SendMeetingInvitationAsync(meetingWithDetails, participantUsers);
        }

        _logger.LogInformation("Meeting created successfully with ID: {MeetingId}", createdMeeting.Id);
        return createdMeeting;
    }

    public async Task<Meeting?> GetMeetingByIdAsync(int id)
    {
        return await _meetingRepository.GetByIdAsync(id);
    }

    public async Task<Meeting?> GetMeetingWithDetailsAsync(int id)
    {
        return await _meetingRepository.GetMeetingWithDetailsAsync(id);
    }

    public async Task<IEnumerable<Meeting>> GetMeetingsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _meetingRepository.GetMeetingsByDateRangeAsync(startDate, endDate);
    }

    public async Task<IEnumerable<Meeting>> GetUserMeetingsAsync(int userId)
    {
        var organizedMeetings = await _meetingRepository.GetMeetingsByOrganizerAsync(userId);
        var participatingMeetings = await _meetingRepository.GetMeetingsByParticipantAsync(userId);

        return organizedMeetings.Union(participatingMeetings)
            .OrderByDescending(m => m.ScheduledDate)
            .ThenByDescending(m => m.StartTime)
            .ToList();
    }

    public async Task<Meeting> UpdateMeetingAsync(int id, UpdateMeetingDto dto)
    {
        _logger.LogInformation("Updating meeting: {MeetingId}", id);

        var meeting = await _meetingRepository.GetByIdAsync(id);
        if (meeting == null)
        {
            throw new MeetingNotFoundException(id);
        }

        // Check for room conflicts if room is changed
        if (dto.MeetingRoomId.HasValue && 
            (dto.MeetingRoomId != meeting.MeetingRoomId || 
             dto.ScheduledDate != meeting.ScheduledDate ||
             dto.StartTime != meeting.StartTime ||
             dto.EndTime != meeting.EndTime))
        {
            var hasConflict = await _meetingRepository.HasRoomConflictAsync(
                dto.MeetingRoomId, dto.ScheduledDate, dto.StartTime, dto.EndTime, id);

            if (hasConflict)
            {
                _logger.LogWarning("Room conflict detected for room {RoomId}", dto.MeetingRoomId);
                throw new RoomNotAvailableException(dto.MeetingRoomId.Value, dto.ScheduledDate, dto.StartTime);
            }
        }

        meeting.Title = dto.Title;
        meeting.Description = dto.Description;
        meeting.ScheduledDate = dto.ScheduledDate;
        meeting.StartTime = dto.StartTime;
        meeting.EndTime = dto.EndTime;
        meeting.MeetingRoomId = dto.MeetingRoomId;
        meeting.Status = dto.Status;
        meeting.UpdatedAt = DateTime.UtcNow;

        await _meetingRepository.UpdateAsync(meeting);

        // Update participants if provided
        if (dto.ParticipantIds.Any())
        {
            var existingParticipants = await _participantRepository.GetParticipantsByMeetingAsync(id);
            var existingUserIds = existingParticipants.Select(p => p.UserId).ToList();

            // Remove participants not in the new list
            var toRemove = existingParticipants.Where(p => !dto.ParticipantIds.Contains(p.UserId));
            foreach (var participant in toRemove)
            {
                await _participantRepository.DeleteAsync(participant);
            }

            // Add new participants
            var toAdd = dto.ParticipantIds.Where(userId => !existingUserIds.Contains(userId))
                .Select(userId => new MeetingParticipant
                {
                    MeetingId = id,
                    UserId = userId,
                    AttendanceStatus = AttendanceStatus.Pending
                });

            if (toAdd.Any())
            {
                await _participantRepository.AddRangeAsync(toAdd);
            }
        }

        _logger.LogInformation("Meeting updated successfully: {MeetingId}", id);
        return meeting;
    }

    public async Task<bool> CancelMeetingAsync(int id, string reason)
    {
        _logger.LogInformation("Cancelling meeting: {MeetingId}, Reason: {Reason}", id, reason);

        var meeting = await _meetingRepository.GetMeetingWithDetailsAsync(id);
        if (meeting == null)
        {
            throw new MeetingNotFoundException(id);
        }

        meeting.Status = MeetingStatus.Cancelled;
        meeting.UpdatedAt = DateTime.UtcNow;

        await _meetingRepository.UpdateAsync(meeting);

        // Cancel scheduled reminders
        await _reminderScheduler.CancelMeetingRemindersAsync(id);

        // Send cancellation notifications
        await _notificationService.SendMeetingCancellationAsync(meeting, reason);

        _logger.LogInformation("Meeting cancelled successfully: {MeetingId}", id);
        return true;
    }

    public async Task<bool> UpdateMeetingStatusAsync(int id, MeetingStatus status)
    {
        _logger.LogInformation("Updating meeting status: {MeetingId} to {Status}", id, status);

        var meeting = await _meetingRepository.GetByIdAsync(id);
        if (meeting == null)
        {
            throw new MeetingNotFoundException(id);
        }

        meeting.Status = status;
        meeting.UpdatedAt = DateTime.UtcNow;

        await _meetingRepository.UpdateAsync(meeting);

        _logger.LogInformation("Meeting status updated successfully: {MeetingId}", id);
        return true;
    }

    public async Task<bool> AddParticipantAsync(int meetingId, int userId)
    {
        _logger.LogInformation("Adding participant {UserId} to meeting {MeetingId}", userId, meetingId);

        var meeting = await _meetingRepository.GetByIdAsync(meetingId);
        if (meeting == null)
        {
            throw new MeetingNotFoundException(meetingId);
        }

        var existingParticipant = await _participantRepository.GetParticipantAsync(meetingId, userId);
        if (existingParticipant != null)
        {
            _logger.LogWarning("User {UserId} is already a participant of meeting {MeetingId}", userId, meetingId);
            return false;
        }

        var participant = new MeetingParticipant
        {
            MeetingId = meetingId,
            UserId = userId,
            AttendanceStatus = AttendanceStatus.Pending
        };

        await _participantRepository.AddAsync(participant);

        _logger.LogInformation("Participant added successfully to meeting {MeetingId}", meetingId);
        return true;
    }

    public async Task<bool> RemoveParticipantAsync(int meetingId, int userId)
    {
        _logger.LogInformation("Removing participant {UserId} from meeting {MeetingId}", userId, meetingId);

        var participant = await _participantRepository.GetParticipantAsync(meetingId, userId);
        if (participant == null)
        {
            _logger.LogWarning("Participant {UserId} not found in meeting {MeetingId}", userId, meetingId);
            return false;
        }

        await _participantRepository.DeleteAsync(participant);

        _logger.LogInformation("Participant removed successfully from meeting {MeetingId}", meetingId);
        return true;
    }

    public async Task<bool> UpdateParticipantStatusAsync(int meetingId, int userId, AttendanceStatus status)
    {
        _logger.LogInformation("Updating participant status for {UserId} in meeting {MeetingId} to {Status}", 
            userId, meetingId, status);

        var participant = await _participantRepository.GetParticipantAsync(meetingId, userId);
        if (participant == null)
        {
            _logger.LogWarning("Participant {UserId} not found in meeting {MeetingId}", userId, meetingId);
            return false;
        }

        participant.AttendanceStatus = status;
        participant.ResponseDate = DateTime.UtcNow;

        await _participantRepository.UpdateAsync(participant);

        // Send attendance confirmation to organizer
        var meeting = await _meetingRepository.GetMeetingWithDetailsAsync(meetingId);
        if (meeting != null && participant.User != null)
        {
            var isAccepted = status == AttendanceStatus.Accepted;
            await _notificationService.SendAttendanceConfirmationAsync(meeting, participant.User, isAccepted);
        }

        _logger.LogInformation("Participant status updated successfully for meeting {MeetingId}", meetingId);
        return true;
    }

    public async Task<IEnumerable<MeetingParticipant>> GetMeetingParticipantsAsync(int meetingId)
    {
        return await _participantRepository.GetParticipantsByMeetingAsync(meetingId);
    }
}
