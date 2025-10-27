using Microsoft.Extensions.Logging;
using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Interfaces;

namespace MeetingManagementSystem.Infrastructure.Services;

public class RoomService : IRoomService
{
    private readonly IMeetingRoomRepository _roomRepository;
    private readonly IMeetingRepository _meetingRepository;
    private readonly ILogger<RoomService> _logger;

    public RoomService(
        IMeetingRoomRepository roomRepository,
        IMeetingRepository meetingRepository,
        ILogger<RoomService> logger)
    {
        _roomRepository = roomRepository;
        _meetingRepository = meetingRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<MeetingRoom>> GetAllRoomsAsync()
    {
        return await _roomRepository.GetAllAsync();
    }

    public async Task<IEnumerable<MeetingRoom>> GetActiveRoomsAsync()
    {
        return await _roomRepository.GetActiveRoomsAsync();
    }

    public async Task<MeetingRoom?> GetRoomByIdAsync(int id)
    {
        return await _roomRepository.GetByIdAsync(id);
    }

    public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeMeetingId = null)
    {
        _logger.LogInformation("Checking availability for room {RoomId} on {Date} from {StartTime} to {EndTime}", 
            roomId, date, startTime, endTime);

        var hasConflict = await _meetingRepository.HasRoomConflictAsync(roomId, date, startTime, endTime, excludeMeetingId);
        
        return !hasConflict;
    }

    public async Task<IEnumerable<MeetingRoom>> GetAvailableRoomsAsync(DateTime date, TimeSpan startTime, TimeSpan endTime)
    {
        _logger.LogInformation("Finding available rooms on {Date} from {StartTime} to {EndTime}", 
            date, startTime, endTime);

        return await _roomRepository.GetAvailableRoomsAsync(date, startTime, endTime);
    }

    public async Task<IEnumerable<RoomAvailabilityDto>> GetRoomAvailabilityDetailsAsync(DateTime date, TimeSpan startTime, TimeSpan endTime)
    {
        _logger.LogInformation("Getting room availability details for {Date} from {StartTime} to {EndTime}", 
            date, startTime, endTime);

        var allRooms = await _roomRepository.GetActiveRoomsAsync();
        var availabilityDetails = new List<RoomAvailabilityDto>();

        foreach (var room in allRooms)
        {
            var isAvailable = await IsRoomAvailableAsync(room.Id, date, startTime, endTime);
            var conflictingSlots = new List<TimeSlot>();

            if (!isAvailable)
            {
                var bookings = await GetRoomBookingsAsync(room.Id, date);
                conflictingSlots = bookings
                    .Where(m => m.StartTime < endTime && m.EndTime > startTime)
                    .Select(m => new TimeSlot
                    {
                        StartTime = m.StartTime,
                        EndTime = m.EndTime,
                        MeetingTitle = m.Title
                    })
                    .ToList();
            }

            availabilityDetails.Add(new RoomAvailabilityDto
            {
                Room = room,
                IsAvailable = isAvailable,
                ConflictingSlots = conflictingSlots
            });
        }

        return availabilityDetails;
    }

    public async Task<IEnumerable<TimeSlot>> GetAlternativeTimeSlotsAsync(int roomId, DateTime date, TimeSpan desiredStartTime, TimeSpan desiredEndTime)
    {
        _logger.LogInformation("Finding alternative time slots for room {RoomId} on {Date}", roomId, date);

        var bookings = await GetRoomBookingsAsync(roomId, date);
        var duration = desiredEndTime - desiredStartTime;
        var alternativeSlots = new List<TimeSlot>();

        // Define working hours (8 AM to 6 PM)
        var workDayStart = new TimeSpan(8, 0, 0);
        var workDayEnd = new TimeSpan(18, 0, 0);

        // Sort bookings by start time
        var sortedBookings = bookings.OrderBy(m => m.StartTime).ToList();

        // Check slot before first booking
        if (sortedBookings.Any())
        {
            var firstBooking = sortedBookings.First();
            if (firstBooking.StartTime > workDayStart)
            {
                var availableTime = firstBooking.StartTime - workDayStart;
                if (availableTime >= duration)
                {
                    alternativeSlots.Add(new TimeSlot
                    {
                        StartTime = workDayStart,
                        EndTime = workDayStart + duration
                    });
                }
            }
        }
        else
        {
            // No bookings, entire day is available
            alternativeSlots.Add(new TimeSlot
            {
                StartTime = workDayStart,
                EndTime = workDayStart + duration
            });
            return alternativeSlots;
        }

        // Check slots between bookings
        for (int i = 0; i < sortedBookings.Count - 1; i++)
        {
            var currentBooking = sortedBookings[i];
            var nextBooking = sortedBookings[i + 1];
            var gapStart = currentBooking.EndTime;
            var gapEnd = nextBooking.StartTime;
            var gapDuration = gapEnd - gapStart;

            if (gapDuration >= duration)
            {
                alternativeSlots.Add(new TimeSlot
                {
                    StartTime = gapStart,
                    EndTime = gapStart + duration
                });
            }
        }

        // Check slot after last booking
        if (sortedBookings.Any())
        {
            var lastBooking = sortedBookings.Last();
            if (lastBooking.EndTime < workDayEnd)
            {
                var availableTime = workDayEnd - lastBooking.EndTime;
                if (availableTime >= duration)
                {
                    alternativeSlots.Add(new TimeSlot
                    {
                        StartTime = lastBooking.EndTime,
                        EndTime = lastBooking.EndTime + duration
                    });
                }
            }
        }

        _logger.LogInformation("Found {Count} alternative time slots for room {RoomId}", 
            alternativeSlots.Count, roomId);

        return alternativeSlots;
    }

    public async Task<IEnumerable<Meeting>> GetRoomBookingsAsync(int roomId, DateTime date)
    {
        return await _meetingRepository.GetMeetingsByRoomAsync(roomId, date);
    }
}
