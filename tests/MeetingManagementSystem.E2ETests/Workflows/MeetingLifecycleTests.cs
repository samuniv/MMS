using MeetingManagementSystem.E2ETests.PageObjects;

namespace MeetingManagementSystem.E2ETests.Workflows;

/// <summary>
/// E2E tests for complete meeting lifecycle workflows using Playwright.
/// Tests: Create meeting → Invite participants → Respond to invitation → Record minutes → Create action items
/// </summary>
[Collection("E2E Collection")]
public class MeetingLifecycleTests : IAsyncLifetime
{
    private readonly PostgreSqlContainerFixture _dbFixture;
    private readonly PlaywrightFixture _playwrightFixture;
    private CustomWebApplicationFactory? _factory;
    private string _baseUrl = "";
    private bool _testFailed;

    public MeetingLifecycleTests(
        PostgreSqlContainerFixture dbFixture,
        PlaywrightFixture playwrightFixture)
    {
        _dbFixture = dbFixture;
        _playwrightFixture = playwrightFixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new CustomWebApplicationFactory(_dbFixture.ConnectionString);
        await _factory.InitializeDatabaseAsync();
        _baseUrl = _factory.Server.BaseAddress.ToString().TrimEnd('/');
    }

    public async Task DisposeAsync()
    {
        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }
    }

    [Theory]
    [InlineData("chromium")]
    [InlineData("firefox")]
    public async Task CreateMeeting_AsGovernmentOfficial_ShouldSucceed(string browserType)
    {
        // Arrange - Generate fresh test data
        var seeder = new DatabaseSeeder(_factory!.Services);
        await seeder.SeedRolesAsync();
        
        var officialEmail = BogusDataGenerator.GenerateEmail("official");
        var officialPassword = "Official@123";
        await seeder.SeedUserAsync(officialEmail, officialPassword, Roles.GovernmentOfficial);
        
        var rooms = await seeder.SeedRoomsAsync(3);
        var room = rooms.First();

        var browser = browserType == "chromium" 
            ? _playwrightFixture.ChromiumBrowser 
            : _playwrightFixture.FirefoxBrowser;
        
        var context = await _playwrightFixture.CreateContextAsync(browser, $"{nameof(CreateMeeting_AsGovernmentOfficial_ShouldSucceed)}_{browserType}");
        var page = await context.NewPageAsync();

        try
        {
            // Login as government official
            var loginPage = new LoginPage(page, _baseUrl);
            await loginPage.NavigateAsync();
            await loginPage.LoginAsync(officialEmail, officialPassword);

            // Act - Create meeting
            var createPage = new MeetingCreatePage(page, _baseUrl);
            await createPage.NavigateAsync();

            var meetingTitle = BogusDataGenerator.GenerateMeeting(1, room.Id).Title;
            var meetingDescription = BogusDataGenerator.GenerateAgenda();
            var startTime = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm");
            var endTime = DateTime.UtcNow.AddDays(1).AddHours(2).ToString("yyyy-MM-ddTHH:mm");

            await createPage.CreateMeetingAsync(
                title: meetingTitle,
                description: meetingDescription,
                startTime: startTime,
                endTime: endTime,
                roomId: room.Id.ToString(),
                agenda: BogusDataGenerator.GenerateAgenda()
            );

            // Assert
            (await createPage.IsOnCreatePageAsync()).Should().BeFalse("user should be redirected after successful meeting creation");
            page.Url.Should().Contain("/Meetings", "user should be redirected to meetings list or details");
            
            _testFailed = false;
        }
        catch
        {
            _testFailed = true;
            throw;
        }
        finally
        {
            await _playwrightFixture.StopTracingAsync(context, $"{nameof(CreateMeeting_AsGovernmentOfficial_ShouldSucceed)}_{browserType}", _testFailed);
            await _factory!.ResetDatabaseAsync();
        }
    }

    [Theory]
    [InlineData("chromium")]
    [InlineData("firefox")]
    public async Task CreateMeeting_WithPastDate_ShouldShowValidationError(string browserType)
    {
        // Arrange - Generate fresh test data
        var seeder = new DatabaseSeeder(_factory!.Services);
        await seeder.SeedRolesAsync();
        
        var officialEmail = BogusDataGenerator.GenerateEmail("official");
        var officialPassword = "Official@123";
        await seeder.SeedUserAsync(officialEmail, officialPassword, Roles.GovernmentOfficial);

        var browser = browserType == "chromium" 
            ? _playwrightFixture.ChromiumBrowser 
            : _playwrightFixture.FirefoxBrowser;
        
        var context = await _playwrightFixture.CreateContextAsync(browser, $"{nameof(CreateMeeting_WithPastDate_ShouldShowValidationError)}_{browserType}");
        var page = await context.NewPageAsync();

        try
        {
            // Login as government official
            var loginPage = new LoginPage(page, _baseUrl);
            await loginPage.NavigateAsync();
            await loginPage.LoginAsync(officialEmail, officialPassword);

            // Act - Attempt to create meeting with past date
            var createPage = new MeetingCreatePage(page, _baseUrl);
            await createPage.NavigateAsync();

            var pastStartTime = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH:mm");
            var pastEndTime = DateTime.UtcNow.AddDays(-1).AddHours(2).ToString("yyyy-MM-ddTHH:mm");

            await createPage.CreateMeetingAsync(
                title: "Past Meeting",
                description: "This should fail",
                startTime: pastStartTime,
                endTime: pastEndTime
            );

            // Assert
            (await createPage.HasValidationErrorsAsync()).Should().BeTrue("validation error should be shown for past date");
            
            _testFailed = false;
        }
        catch
        {
            _testFailed = true;
            throw;
        }
        finally
        {
            await _playwrightFixture.StopTracingAsync(context, $"{nameof(CreateMeeting_WithPastDate_ShouldShowValidationError)}_{browserType}", _testFailed);
            await _factory!.ResetDatabaseAsync();
        }
    }

    [Theory]
    [InlineData("chromium")]
    [InlineData("firefox")]
    public async Task CreateMeeting_AsParticipant_ShouldBeDenied(string browserType)
    {
        // Arrange - Generate fresh test data
        var seeder = new DatabaseSeeder(_factory!.Services);
        await seeder.SeedRolesAsync();
        
        var participantEmail = BogusDataGenerator.GenerateEmail("participant");
        var participantPassword = "Participant@123";
        await seeder.SeedUserAsync(participantEmail, participantPassword, Roles.Participant);

        var browser = browserType == "chromium" 
            ? _playwrightFixture.ChromiumBrowser 
            : _playwrightFixture.FirefoxBrowser;
        
        var context = await _playwrightFixture.CreateContextAsync(browser, $"{nameof(CreateMeeting_AsParticipant_ShouldBeDenied)}_{browserType}");
        var page = await context.NewPageAsync();

        try
        {
            // Login as participant
            var loginPage = new LoginPage(page, _baseUrl);
            await loginPage.NavigateAsync();
            await loginPage.LoginAsync(participantEmail, participantPassword);

            // Act - Attempt to navigate to create meeting page
            await page.GotoAsync($"{_baseUrl}/Meetings/Create");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert
            page.Url.Should().Contain("/Account/AccessDenied", "participant should be denied access to create meetings");
            
            _testFailed = false;
        }
        catch
        {
            _testFailed = true;
            throw;
        }
        finally
        {
            await _playwrightFixture.StopTracingAsync(context, $"{nameof(CreateMeeting_AsParticipant_ShouldBeDenied)}_{browserType}", _testFailed);
            await _factory!.ResetDatabaseAsync();
        }
    }
}
