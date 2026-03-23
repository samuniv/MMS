using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MeetingManagementSystem.Infrastructure.Data;
using System.Security.Claims;
using System.Net.Http.Headers;
using AngleSharp.Html.Dom;

namespace MeetingManagementSystem.E2ETests.Helpers;

/// <summary>
/// Helper class for authentication-related operations in E2E tests.
/// Supports creating authenticated HTTP clients and browser contexts.
/// </summary>
public class AuthenticationHelper
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthenticationHelper(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Create a test user with the specified role and return the user entity.
    /// </summary>
    public async Task<User> CreateTestUserAsync(string email, string password, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

        // Ensure role exists
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<int>(role));
        }

        // Create user
        var user = new User
        {
            UserName = email,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            Department = "Test Department",
            Position = "Test Position",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // Add role
        await userManager.AddToRoleAsync(user, role);

        return user;
    }

    /// <summary>
    /// Create an authenticated HttpClient for HTTP integration tests.
    /// </summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        // Perform login
        var loginPage = await client.GetAsync("/Account/Login");
        var loginContent = await loginPage.Content.ReadAsStringAsync();
        var antiforgeryToken = FormHelper.ExtractAntiForgeryToken(loginContent);

        var loginData = new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["__RequestVerificationToken"] = antiforgeryToken
        };

        var loginResponse = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(loginData));

        if (loginResponse.StatusCode != System.Net.HttpStatusCode.Redirect &&
            loginResponse.StatusCode != System.Net.HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"Login failed with status code: {loginResponse.StatusCode}");
        }

        return client;
    }

    /// <summary>
    /// Login to the application using Playwright browser context.
    /// </summary>
    public async Task LoginWithBrowserAsync(IPage page, string baseUrl, string email, string password)
    {
        await page.GotoAsync($"{baseUrl}/Account/Login");
        
        await page.FillAsync("input[name='Input.Email']", email);
        await page.FillAsync("input[name='Input.Password']", password);
        
        await page.ClickAsync("button[type='submit']");
        
        // Wait for navigation after login
        await page.WaitForURLAsync($"{baseUrl}/**", new PageWaitForURLOptions 
        { 
            WaitUntil = WaitUntilState.NetworkIdle 
        });
    }

    /// <summary>
    /// Logout from the application using Playwright browser context.
    /// </summary>
    public async Task LogoutWithBrowserAsync(IPage page, string baseUrl)
    {
        await page.GotoAsync($"{baseUrl}/Account/Logout");
        
        // Click the logout button if present
        var logoutButton = await page.QuerySelectorAsync("button[type='submit']");
        if (logoutButton != null)
        {
            await logoutButton.ClickAsync();
        }

        await page.WaitForURLAsync($"{baseUrl}/Account/Login", new PageWaitForURLOptions 
        { 
            WaitUntil = WaitUntilState.NetworkIdle 
        });
    }

    /// <summary>
    /// Seed standard test users (admin, official, participant) for testing.
    /// </summary>
    public async Task<Dictionary<string, User>> SeedStandardTestUsersAsync()
    {
        var users = new Dictionary<string, User>();

        // Create admin user
        var adminEmail = BogusDataGenerator.GenerateEmail("admin");
        var adminPassword = "Admin@123";
        users["admin"] = await CreateTestUserAsync(adminEmail, adminPassword, Roles.Administrator);

        // Create government official user
        var officialEmail = BogusDataGenerator.GenerateEmail("official");
        var officialPassword = "Official@123";
        users["official"] = await CreateTestUserAsync(officialEmail, officialPassword, Roles.GovernmentOfficial);

        // Create participant user
        var participantEmail = BogusDataGenerator.GenerateEmail("participant");
        var participantPassword = "Participant@123";
        users["participant"] = await CreateTestUserAsync(participantEmail, participantPassword, Roles.Participant);

        return users;
    }
}
