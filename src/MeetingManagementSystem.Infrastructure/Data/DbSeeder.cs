using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Enums;

namespace MeetingManagementSystem.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, UserManager<User> userManager, RoleManager<IdentityRole<int>> roleManager)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Seed Roles
        await SeedRolesAsync(roleManager);

        // Seed Users
        await SeedUsersAsync(userManager);

        // Seed Meeting Rooms
        await SeedMeetingRoomsAsync(context);

        await context.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<int>> roleManager)
    {
        var roles = new[]
        {
            "Administrator",
            "Government Official", 
            "Participant"
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<int> { Name = role });
            }
        }
    }

    private static async Task SeedUsersAsync(UserManager<User> userManager)
    {
        // Seed Administrator
        var adminEmail = "admin@gov.np";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Administrator",
                Department = "IT Department",
                Position = "System Administrator",
                OfficeLocation = "Main Building - IT Floor",
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(admin, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Administrator");
            }
        }

        // Seed Government Official
        var officialEmail = "official@gov.np";
        if (await userManager.FindByEmailAsync(officialEmail) == null)
        {
            var official = new User
            {
                UserName = officialEmail,
                Email = officialEmail,
                FirstName = "Ram",
                LastName = "Sharma",
                Department = "Planning Department",
                Position = "Senior Officer",
                OfficeLocation = "Main Building - 2nd Floor",
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(official, "Official@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(official, "Government Official");
            }
        }

        // Seed Participant
        var participantEmail = "participant@gov.np";
        if (await userManager.FindByEmailAsync(participantEmail) == null)
        {
            var participant = new User
            {
                UserName = participantEmail,
                Email = participantEmail,
                FirstName = "Sita",
                LastName = "Poudel",
                Department = "Finance Department",
                Position = "Assistant Officer",
                OfficeLocation = "Main Building - 1st Floor",
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(participant, "Participant@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(participant, "Participant");
            }
        }
    }

    private static async Task SeedMeetingRoomsAsync(ApplicationDbContext context)
    {
        if (!await context.MeetingRooms.AnyAsync())
        {
            var meetingRooms = new[]
            {
                new MeetingRoom
                {
                    Name = "Conference Room A",
                    Location = "Main Building - 3rd Floor",
                    Capacity = 20,
                    Equipment = "Projector, Whiteboard, Video Conferencing, Air Conditioning",
                    IsActive = true
                },
                new MeetingRoom
                {
                    Name = "Conference Room B",
                    Location = "Main Building - 3rd Floor",
                    Capacity = 12,
                    Equipment = "Projector, Whiteboard, Air Conditioning",
                    IsActive = true
                },
                new MeetingRoom
                {
                    Name = "Board Room",
                    Location = "Main Building - 4th Floor",
                    Capacity = 30,
                    Equipment = "Large Screen Display, Video Conferencing, Sound System, Air Conditioning",
                    IsActive = true
                },
                new MeetingRoom
                {
                    Name = "Small Meeting Room",
                    Location = "Main Building - 2nd Floor",
                    Capacity = 6,
                    Equipment = "Whiteboard, Air Conditioning",
                    IsActive = true
                },
                new MeetingRoom
                {
                    Name = "Training Hall",
                    Location = "Annex Building - Ground Floor",
                    Capacity = 50,
                    Equipment = "Projector, Sound System, Microphones, Air Conditioning",
                    IsActive = true
                }
            };

            await context.MeetingRooms.AddRangeAsync(meetingRooms);
        }
    }
}