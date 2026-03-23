namespace MeetingManagementSystem.E2ETests.Integration;

/// <summary>
/// Integration tests for CSRF (Cross-Site Request Forgery) protection.
/// Tests that all POST endpoints require valid antiforgery tokens.
/// </summary>
[Collection("E2E Collection")]
public class CsrfProtectionTests : IAsyncLifetime
{
    private readonly PostgreSqlContainerFixture _dbFixture;
    private CustomWebApplicationFactory? _factory;

    public CsrfProtectionTests(PostgreSqlContainerFixture dbFixture)
    {
        _dbFixture = dbFixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new CustomWebApplicationFactory(_dbFixture.ConnectionString);
        await _factory.InitializeDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }
    }

    [Fact]
    public async Task PostLogin_WithoutAntiforgeryToken_ShouldFail()
    {
        // Arrange
        var client = _factory!.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        try
        {
            // Act - Attempt POST without antiforgery token
            var loginData = new Dictionary<string, string>
            {
                ["Input.Email"] = "test@gov.np",
                ["Input.Password"] = "Test@123"
            };

            var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(loginData));

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest, "POST without antiforgery token should be rejected");
        }
        finally
        {
            await _factory.ResetDatabaseAsync();
        }
    }

    [Fact]
    public async Task PostLogin_WithValidAntiforgeryToken_ShouldSucceed()
    {
        // Arrange - Generate fresh test data
        var seeder = new DatabaseSeeder(_factory!.Services);
        await seeder.SeedRolesAsync();
        
        var email = BogusDataGenerator.GenerateEmail("testuser");
        var password = "Test@123";
        await seeder.SeedUserAsync(email, password, Roles.Participant);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        try
        {
            // Get login page to obtain antiforgery token
            var loginPage = await client.GetAsync("/Account/Login");
            var loginContent = await loginPage.Content.ReadAsStringAsync();
            var antiforgeryToken = FormHelper.ExtractAntiForgeryToken(loginContent);

            // Act - POST with valid antiforgery token
            var loginData = new Dictionary<string, string>
            {
                ["Input.Email"] = email,
                ["Input.Password"] = password,
                ["__RequestVerificationToken"] = antiforgeryToken
            };

            var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(loginData));

            // Assert
            response.StatusCode.Should().BeOneOf(System.Net.HttpStatusCode.OK, System.Net.HttpStatusCode.Redirect);
        }
        finally
        {
            await _factory.ResetDatabaseAsync();
        }
    }

    [Fact]
    public async Task PostMeetingCreate_WithoutAntiforgeryToken_ShouldFail()
    {
        // Arrange - Generate fresh test data
        var seeder = new DatabaseSeeder(_factory!.Services);
        await seeder.SeedRolesAsync();
        
        var officialEmail = BogusDataGenerator.GenerateEmail("official");
        var officialPassword = "Official@123";
        await seeder.SeedUserAsync(officialEmail, officialPassword, Roles.GovernmentOfficial);

        var authHelper = new AuthenticationHelper(_factory);
        var client = await authHelper.CreateAuthenticatedClientAsync(officialEmail, officialPassword);

        try
        {
            // Act - Attempt POST without antiforgery token
            var meetingData = new Dictionary<string, string>
            {
                ["Input.Title"] = "Test Meeting",
                ["Input.Description"] = "Test Description",
                ["Input.StartTime"] = DateTime.UtcNow.AddDays(1).ToString("o"),
                ["Input.EndTime"] = DateTime.UtcNow.AddDays(1).AddHours(2).ToString("o")
            };

            var response = await client.PostAsync("/Meetings/Create", new FormUrlEncodedContent(meetingData));

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest, "POST without antiforgery token should be rejected");
        }
        finally
        {
            await _factory.ResetDatabaseAsync();
        }
    }

    [Fact]
    public async Task PostMeetingCreate_WithValidAntiforgeryToken_ShouldSucceed()
    {
        // Arrange - Generate fresh test data
        var seeder = new DatabaseSeeder(_factory!.Services);
        await seeder.SeedRolesAsync();
        
        var officialEmail = BogusDataGenerator.GenerateEmail("official");
        var officialPassword = "Official@123";
        var official = await seeder.SeedUserAsync(officialEmail, officialPassword, Roles.GovernmentOfficial);

        var authHelper = new AuthenticationHelper(_factory);
        var client = await authHelper.CreateAuthenticatedClientAsync(officialEmail, officialPassword);

        try
        {
            // Get create page to obtain antiforgery token
            var createPage = await client.GetAsync("/Meetings/Create");
            var createContent = await createPage.Content.ReadAsStringAsync();
            var antiforgeryToken = FormHelper.ExtractAntiForgeryToken(createContent);

            // Act - POST with valid antiforgery token
            var meetingData = new Dictionary<string, string>
            {
                ["Input.Title"] = "Test Meeting",
                ["Input.Description"] = "Test Description",
                ["Input.StartTime"] = DateTime.UtcNow.AddDays(1).ToString("o"),
                ["Input.EndTime"] = DateTime.UtcNow.AddDays(1).AddHours(2).ToString("o"),
                ["__RequestVerificationToken"] = antiforgeryToken
            };

            var response = await client.PostAsync("/Meetings/Create", new FormUrlEncodedContent(meetingData));

            // Assert
            response.StatusCode.Should().BeOneOf(System.Net.HttpStatusCode.OK, System.Net.HttpStatusCode.Redirect);
        }
        finally
        {
            await _factory.ResetDatabaseAsync();
        }
    }

    [Fact]
    public async Task PostWithInvalidAntiforgeryToken_ShouldFail()
    {
        // Arrange - Generate fresh test data
        var seeder = new DatabaseSeeder(_factory!.Services);
        await seeder.SeedRolesAsync();
        
        var email = BogusDataGenerator.GenerateEmail("testuser");
        var password = "Test@123";
        await seeder.SeedUserAsync(email, password, Roles.Participant);

        var client = _factory!.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        try
        {
            // Get login page
            var loginPage = await client.GetAsync("/Account/Login");

            // Act - POST with invalid antiforgery token
            var loginData = new Dictionary<string, string>
            {
                ["Input.Email"] = email,
                ["Input.Password"] = password,
                ["__RequestVerificationToken"] = "invalid-token-12345"
            };

            var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(loginData));

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest, "POST with invalid antiforgery token should be rejected");
        }
        finally
        {
            await _factory.ResetDatabaseAsync();
        }
    }
}
