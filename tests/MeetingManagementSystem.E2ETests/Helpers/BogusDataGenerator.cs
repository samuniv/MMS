using Bogus;
using MeetingManagementSystem.Core.Enums;

namespace MeetingManagementSystem.E2ETests.Helpers;

/// <summary>
/// Uses Bogus library to generate realistic test data for E2E tests.
/// Generates fresh data for each test to ensure isolation.
/// </summary>
public static class BogusDataGenerator
{
    /// <summary>
    /// Generate a test user with realistic data.
    /// </summary>
    public static User GenerateUser(string? role = null)
    {
        var faker = new Faker();
        var user = new User
        {
            UserName = faker.Internet.Email(),
            Email = faker.Internet.Email(),
            NormalizedEmail = faker.Internet.Email().ToUpper(),
            NormalizedUserName = faker.Internet.Email().ToUpper(),
            FirstName = faker.Name.FirstName(),
            LastName = faker.Name.LastName(),
            Department = faker.Commerce.Department(),
            Position = faker.Name.JobTitle(),
            PhoneNumber = faker.Phone.PhoneNumber(),
            EmailConfirmed = true,
            PhoneNumberConfirmed = false,
            TwoFactorEnabled = false,
            LockoutEnabled = true,
            AccessFailedCount = 0,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        return user;
    }

    /// <summary>
    /// Generate multiple test users with different roles.
    /// </summary>
    public static List<User> GenerateUsers(int count)
    {
        var faker = new Faker();
        return Enumerable.Range(1, count)
            .Select(_ => GenerateUser())
            .ToList();
    }

    /// <summary>
    /// Generate a meeting room with realistic data.
    /// </summary>
    public static MeetingRoom GenerateRoom()
    {
        var faker = new Faker();
        return new MeetingRoom
        {
            Name = $"{faker.Company.CompanyName()} Room",
            Capacity = faker.Random.Int(5, 50),
            Location = $"{faker.Address.StreetAddress()}, Floor {faker.Random.Int(1, 5)}",
            Equipment = string.Join(", ", faker.Random.ListItems(new[] { "Projector", "Whiteboard", "Video Conference", "Microphone" }, faker.Random.Int(1, 3))),
            IsActive = true
        };
    }

    /// <summary>
    /// Generate multiple meeting rooms.
    /// </summary>
    public static List<MeetingRoom> GenerateRooms(int count)
    {
        return Enumerable.Range(1, count)
            .Select(_ => GenerateRoom())
            .ToList();
    }

    /// <summary>
    /// Generate a meeting with realistic data.
    /// </summary>
    public static Meeting GenerateMeeting(int organizerId, int? roomId = null)
    {
        var faker = new Faker();
        var scheduledDate = faker.Date.Future(refDate: DateTime.UtcNow.AddHours(2));
        var startHour = faker.Random.Int(8, 16);
        
        return new Meeting
        {
            Title = faker.Company.CatchPhrase(),
            Description = faker.Lorem.Paragraph(),
            ScheduledDate = scheduledDate.Date,
            StartTime = TimeSpan.FromHours(startHour),
            EndTime = TimeSpan.FromHours(startHour + faker.Random.Int(1, 4)),
            MeetingRoomId = roomId,
            OrganizerId = organizerId,
            Status = MeetingStatus.Scheduled
        };
    }

    /// <summary>
    /// Generate meeting agenda items.
    /// </summary>
    public static string GenerateAgenda()
    {
        var faker = new Faker();
        var items = Enumerable.Range(1, faker.Random.Int(3, 7))
            .Select(i => $"{i}. {faker.Lorem.Sentence()}")
            .ToList();
        
        return string.Join("\n", items);
    }

    /// <summary>
    /// Generate action item with realistic data.
    /// </summary>
    public static ActionItem GenerateActionItem(int agendaItemId, int assignedToId)
    {
        var faker = new Faker();
        return new ActionItem
        {
            AgendaItemId = agendaItemId,
            Description = faker.Lorem.Paragraph(),
            AssignedToId = assignedToId,
            DueDate = faker.Date.Future(),
            Status = ActionItemStatus.Pending
        };
    }

    /// <summary>
    /// Generate meeting minutes with realistic data.
    /// </summary>
    public static MeetingMinutes GenerateMeetingMinutes(int meetingId, int createdById)
    {
        var faker = new Faker();
        return new MeetingMinutes
        {
            MeetingId = meetingId,
            Content = faker.Lorem.Paragraphs(3),
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Generate a valid password that meets the application's requirements.
    /// </summary>
    public static string GenerateValidPassword()
    {
        var faker = new Faker();
        // Password must be at least 8 characters with uppercase, lowercase, and digit
        return $"{faker.Internet.Password(8, false, "\\w", "Aa1")}!";
    }

    /// <summary>
    /// Generate test email address with a specific domain.
    /// </summary>
    public static string GenerateEmail(string? prefix = null)
    {
        var faker = new Faker();
        var name = prefix ?? faker.Internet.UserName();
        return $"{name}@gov.np";
    }

    /// <summary>
    /// Generate edge case strings for validation testing.
    /// </summary>
    public static class EdgeCases
    {
        public static string VeryLongString(int length = 1000)
        {
            return new string('A', length);
        }

        public static string StringWithSpecialCharacters()
        {
            return "Test<>\"'&%;$#@!{}[]";
        }

        public static string StringWithUnicode()
        {
            return "Test नेपाली भाषा 测试 テスト";
        }

        public static string EmptyString() => "";

        public static string WhitespaceString() => "   ";
    }
}
