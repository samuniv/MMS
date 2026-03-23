using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MeetingManagementSystem.Infrastructure.Data;

namespace MeetingManagementSystem.E2ETests.Helpers;

/// <summary>
/// Helper class to seed database with test data using Bogus generators.
/// Generates fresh data for each test to ensure isolation.
/// </summary>
public class DatabaseSeeder
{
    private readonly IServiceProvider _serviceProvider;

    public DatabaseSeeder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Seed roles required by the application.
    /// </summary>
    public async Task SeedRolesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

        var roles = new[] { Roles.Administrator, Roles.GovernmentOfficial, Roles.Participant };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(roleName));
            }
        }
    }

    /// <summary>
    /// Seed a user with a specific role.
    /// </summary>
    public async Task<User> SeedUserAsync(string email, string password, string role)
    {
        using var scope = _serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        // Check if user already exists
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            return existingUser;
        }

        var user = BogusDataGenerator.GenerateUser(role);
        user.UserName = email;
        user.Email = email;
        user.NormalizedEmail = email.ToUpper();
        user.NormalizedUserName = email.ToUpper();

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        await userManager.AddToRoleAsync(user, role);

        return user;
    }

    /// <summary>
    /// Seed multiple users.
    /// </summary>
    public async Task<List<User>> SeedUsersAsync(int count, string role)
    {
        var users = new List<User>();
        for (int i = 0; i < count; i++)
        {
            var email = BogusDataGenerator.GenerateEmail($"user{i}");
            var password = BogusDataGenerator.GenerateValidPassword();
            users.Add(await SeedUserAsync(email, password, role));
        }
        return users;
    }

    /// <summary>
    /// Seed meeting rooms.
    /// </summary>
    public async Task<List<MeetingRoom>> SeedRoomsAsync(int count)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var rooms = BogusDataGenerator.GenerateRooms(count);
        await context.MeetingRooms.AddRangeAsync(rooms);
        await context.SaveChangesAsync();

        return rooms;
    }

    /// <summary>
    /// Seed a meeting with participants.
    /// </summary>
    public async Task<Meeting> SeedMeetingAsync(int organizerId, int? roomId = null, List<int>? participantIds = null)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var meeting = BogusDataGenerator.GenerateMeeting(organizerId, roomId);
        await context.Meetings.AddAsync(meeting);
        await context.SaveChangesAsync();

        // Add participants if provided
        if (participantIds != null && participantIds.Any())
        {
            var participants = participantIds.Select(id => new MeetingParticipant
            {
                MeetingId = meeting.Id,
                UserId = id,
                AttendanceStatus = Core.Enums.AttendanceStatus.Pending
            }).ToList();

            await context.MeetingParticipants.AddRangeAsync(participants);
            await context.SaveChangesAsync();
        }

        return meeting;
    }

    /// <summary>
    /// Seed action items for agenda items.
    /// </summary>
    public async Task<List<ActionItem>> SeedActionItemsAsync(int agendaItemId, List<int> assignedToIds)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var actionItems = assignedToIds
            .Select(userId => BogusDataGenerator.GenerateActionItem(agendaItemId, userId))
            .ToList();

        await context.ActionItems.AddRangeAsync(actionItems);
        await context.SaveChangesAsync();

        return actionItems;
    }

    /// <summary>
    /// Seed meeting minutes for a meeting.
    /// </summary>
    public async Task<MeetingMinutes> SeedMeetingMinutesAsync(int meetingId, int createdById)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var minutes = BogusDataGenerator.GenerateMeetingMinutes(meetingId, createdById);
        await context.MeetingMinutes.AddAsync(minutes);
        await context.SaveChangesAsync();

        return minutes;
    }
}
