using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace MeetingManagementSystem.Infrastructure.Services;

public class MeetingMinutesService : IMeetingMinutesService
{
    private readonly IMeetingMinutesRepository _minutesRepository;
    private readonly IMeetingRepository _meetingRepository;
    private readonly ILogger<MeetingMinutesService> _logger;

    public MeetingMinutesService(
        IMeetingMinutesRepository minutesRepository,
        IMeetingRepository meetingRepository,
        ILogger<MeetingMinutesService> logger)
    {
        _minutesRepository = minutesRepository;
        _meetingRepository = meetingRepository;
        _logger = logger;
    }

    public async Task<MeetingMinutes> CreateMinutesAsync(CreateMeetingMinutesDto dto)
    {
        var meeting = await _meetingRepository.GetByIdAsync(dto.MeetingId);
        if (meeting == null)
        {
            throw new ArgumentException($"Meeting with ID {dto.MeetingId} not found");
        }

        var minutes = new MeetingMinutes
        {
            MeetingId = dto.MeetingId,
            Content = dto.Content,
            CreatedById = dto.CreatedById,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };

        var createdMinutes = await _minutesRepository.AddAsync(minutes);
        _logger.LogInformation("Meeting minutes created for meeting {MeetingId} by user {UserId}", 
            dto.MeetingId, dto.CreatedById);

        return createdMinutes;
    }

    public async Task<MeetingMinutes?> GetMinutesByIdAsync(int id)
    {
        return await _minutesRepository.GetByIdAsync(id);
    }

    public async Task<MeetingMinutes?> GetMinutesByMeetingIdAsync(int meetingId)
    {
        return await _minutesRepository.GetByMeetingIdAsync(meetingId);
    }

    public async Task<MeetingMinutes> UpdateMinutesAsync(int id, UpdateMeetingMinutesDto dto)
    {
        var minutes = await _minutesRepository.GetByIdAsync(id);
        if (minutes == null)
        {
            throw new ArgumentException($"Meeting minutes with ID {id} not found");
        }

        minutes.Content = dto.Content;
        minutes.LastModified = DateTime.UtcNow;

        await _minutesRepository.UpdateAsync(minutes);
        _logger.LogInformation("Meeting minutes {MinutesId} updated", id);

        return minutes;
    }

    public async Task<bool> DeleteMinutesAsync(int id)
    {
        var minutes = await _minutesRepository.GetByIdAsync(id);
        if (minutes == null)
        {
            return false;
        }

        await _minutesRepository.DeleteAsync(minutes);
        _logger.LogInformation("Meeting minutes {MinutesId} deleted", id);

        return true;
    }

    public async Task<byte[]> ExportMinutesToPdfAsync(int minutesId)
    {
        var minutes = await _minutesRepository.GetByIdAsync(minutesId);
        if (minutes == null)
        {
            throw new ArgumentException($"Meeting minutes with ID {minutesId} not found");
        }

        // For now, return a simple text-based representation
        // In a real implementation, you would use a PDF library like QuestPDF or iTextSharp
        var content = $"Meeting Minutes\n\nMeeting ID: {minutes.MeetingId}\n" +
                     $"Created: {minutes.CreatedAt}\n" +
                     $"Last Modified: {minutes.LastModified}\n\n" +
                     $"Content:\n{minutes.Content}";

        return System.Text.Encoding.UTF8.GetBytes(content);
    }
}
