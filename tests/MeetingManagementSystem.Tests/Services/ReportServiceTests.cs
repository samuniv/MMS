using Microsoft.EntityFrameworkCore;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Enums;
using MeetingManagementSystem.Infrastructure.Data;
using MeetingManagementSystem.Infrastructure.Services;

namespace MeetingManagementSystem.Tests.Services;

public class ReportServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ReportService _reportService;

    public ReportServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _reportService = new ReportService(_context);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var users = new List<User>
        {
            new User { Id = 1, FirstName = "John", LastName = "Doe", Department = "IT", Email = "john@test.com", UserName = "john@test.com", IsActive = true },
            new User { Id = 2, FirstName = "Jane", LastName = "Smith", Department = "HR", Email = "jane@test.com", UserName = "jane@test.com", IsActive = true },
            new User { Id = 3, FirstName = "Bob", LastName = "Johnson", Department = "IT", Email = "bob@test.com", UserName = "bob@test.com", IsActive = true }
        };

        var rooms = new List<MeetingRoom>
        {
            new MeetingRoom { Id = 1, Name = "Conference Room A", Location = "Floor 1", Capacity = 10, IsActive = true },
            new MeetingRoom { Id = 2, Name = "Conference Room B", Location = "Floor 2", Capacity = 20, IsActive = true }
        };

        var meetings = new List<Meeting>
        {
            new Meeting
            {
                Id = 1,
                Title = "Team Meeting",
                ScheduledDate = DateTime.Today.AddDays(-5),
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                OrganizerId = 1,
                MeetingRoomId = 1,
                Status = MeetingStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-6)
            },
            new Meeting
            {
                Id = 2,
                Title = "Project Review",
                ScheduledDate = DateTime.Today.AddDays(-3),
                StartTime = new TimeSpan(14, 0, 0),
                EndTime = new TimeSpan(16, 0, 0),
                OrganizerId = 2,
                MeetingRoomId = 2,
                Status = MeetingStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-4)
            },
            new Meeting
            {
                Id = 3,
                Title = "Planning Session",
                ScheduledDate = DateTime.Today.AddDays(2),
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(10, 0, 0),
                OrganizerId = 1,
                MeetingRoomId = 1,
                Status = MeetingStatus.Scheduled,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            }
        };

        var participants = new List<MeetingParticipant>
        {
            new MeetingParticipant { Id = 1, MeetingId = 1, UserId = 2, AttendanceStatus = AttendanceStatus.Attended },
            new MeetingParticipant { Id = 2, MeetingId = 1, UserId = 3, AttendanceStatus = AttendanceStatus.Attended },
            new MeetingParticipant { Id = 3, MeetingId = 2, UserId = 1, AttendanceStatus = AttendanceStatus.Attended },
            new MeetingParticipant { Id = 4, MeetingId = 2, UserId = 3, AttendanceStatus = AttendanceStatus.Absent },
            new MeetingParticipant { Id = 5, MeetingId = 3, UserId = 2, AttendanceStatus = AttendanceStatus.Pending }
        };

        var documents = new List<MeetingDocument>
        {
            new MeetingDocument { Id = 1, MeetingId = 1, FileName = "doc1.pdf", FileSize = 1024, UploadedAt = DateTime.UtcNow.AddDays(-5), UploadedById = 1 },
            new MeetingDocument { Id = 2, MeetingId = 2, FileName = "doc2.pdf", FileSize = 2048, UploadedAt = DateTime.UtcNow.AddDays(-3), UploadedById = 2 }
        };

        _context.Users.AddRange(users);
        _context.MeetingRooms.AddRange(rooms);
        _context.Meetings.AddRange(meetings);
        _context.MeetingParticipants.AddRange(participants);
        _context.MeetingDocuments.AddRange(documents);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetMeetingAttendanceReportAsync_ReturnsCorrectData()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-10);
        var endDate = DateTime.Today;

        // Act
        var result = await _reportService.GetMeetingAttendanceReportAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        var reportList = result.ToList();
        Assert.Equal(2, reportList.Count); // Only completed meetings in the past
        
        var firstMeeting = reportList.First(r => r.MeetingId == 1);
        Assert.Equal("Team Meeting", firstMeeting.MeetingTitle);
        Assert.Equal(2, firstMeeting.TotalParticipants);
        Assert.Equal(2, firstMeeting.AttendedCount);
        Assert.Equal(100.0, firstMeeting.AttendanceRate);
    }

    [Fact]
    public async Task GetMeetingAttendanceReportAsync_FiltersByDepartment()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-10);
        var endDate = DateTime.Today;
        var department = "IT";

        // Act
        var result = await _reportService.GetMeetingAttendanceReportAsync(startDate, endDate, department);

        // Assert
        Assert.NotNull(result);
        var reportList = result.ToList();
        Assert.Single(reportList); // Only meetings organized by IT department
        Assert.Equal("Team Meeting", reportList[0].MeetingTitle);
    }

    [Fact]
    public async Task GetRoomUtilizationReportAsync_ReturnsCorrectData()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-10);
        var endDate = DateTime.Today.AddDays(10);

        // Act
        var result = await _reportService.GetRoomUtilizationReportAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        var reportList = result.ToList();
        Assert.Equal(2, reportList.Count);
        
        var roomA = reportList.First(r => r.RoomId == 1);
        Assert.Equal("Conference Room A", roomA.RoomName);
        Assert.Equal(2, roomA.TotalBookings);
        Assert.Equal(1, roomA.CompletedMeetings);
    }

    [Fact]
    public async Task GetMeetingStatisticsAsync_ReturnsCorrectMetrics()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-10);
        var endDate = DateTime.Today.AddDays(10);

        // Act
        var result = await _reportService.GetMeetingStatisticsAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalMeetings);
        Assert.Equal(1, result.ScheduledMeetings);
        Assert.Equal(2, result.CompletedMeetings);
        Assert.Equal(0, result.CancelledMeetings);
        Assert.True(result.AverageDurationMinutes > 0);
        Assert.Equal(2, result.TotalDocuments);
    }

    [Fact]
    public async Task GetMeetingStatisticsAsync_FiltersByDepartment()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-10);
        var endDate = DateTime.Today.AddDays(10);
        var department = "IT";

        // Act
        var result = await _reportService.GetMeetingStatisticsAsync(startDate, endDate, department);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalMeetings); // Only meetings organized by IT
        Assert.Contains("IT", result.MeetingsByDepartment.Keys);
    }

    [Fact]
    public async Task GetUserActivityReportAsync_ReturnsCorrectData()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-10);
        var endDate = DateTime.Today.AddDays(10);

        // Act
        var result = await _reportService.GetUserActivityReportAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        var reportList = result.ToList();
        Assert.True(reportList.Count > 0);
        
        var johnDoe = reportList.FirstOrDefault(u => u.UserName.Contains("John"));
        Assert.NotNull(johnDoe);
        Assert.Equal(2, johnDoe.MeetingsOrganized);
    }

    [Fact]
    public async Task ExportMeetingAttendanceReportToPdfAsync_ReturnsHtmlContent()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-10);
        var endDate = DateTime.Today;

        // Act
        var result = await _reportService.ExportMeetingAttendanceReportToPdfAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        var html = System.Text.Encoding.UTF8.GetString(result);
        Assert.Contains("Meeting Attendance Report", html);
        Assert.Contains("<table>", html);
    }

    [Fact]
    public async Task ExportRoomUtilizationReportToExcelAsync_ReturnsCsvContent()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-10);
        var endDate = DateTime.Today.AddDays(10);

        // Act
        var result = await _reportService.ExportRoomUtilizationReportToExcelAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        var csv = System.Text.Encoding.UTF8.GetString(result);
        Assert.Contains("Room Utilization Report", csv);
        Assert.Contains("Conference Room A", csv);
    }

    [Fact]
    public async Task ExportMeetingStatisticsToExcelAsync_ReturnsCsvContent()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-10);
        var endDate = DateTime.Today.AddDays(10);

        // Act
        var result = await _reportService.ExportMeetingStatisticsToExcelAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        var csv = System.Text.Encoding.UTF8.GetString(result);
        Assert.Contains("Meeting Statistics Report", csv);
        Assert.Contains("Total Meetings", csv);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
