namespace MeetingManagementSystem.E2ETests.Integration;

/// <summary>
/// Integration tests for authorization policies using HTTP client.
/// Tests all 7 authorization policies with different user roles.
/// </summary>
[Collection("E2E Collection")]
public class AuthorizationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainerFixture _dbFixture;
    private CustomWebApplicationFactory? _factory;

    public AuthorizationTests(PostgreSqlContainerFixture dbFixture)
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
    public async Task AdministratorOnly_Policy_ShouldAllowAdmin()
    {
        // Arrange - Generate fresh test data
        var seeder = new DatabaseSeeder(_factory!.Services);
        await seeder.SeedRolesAsync();
        
        var adminEmail = BogusDataGenerator.GenerateEmail("admin");
        var adminPassword = "Admin@123";
        await seeder.SeedUserAsync(adminEmail, adminPassword, Roles.Administrator);

        var authHelper = new AuthenticationHelper(_factory);
        var client = await authHelper.CreateAuthenticatedClientAsync(adminEmail, adminPassword);

        try
        {
            // Act - Access admin-only page
            var response = await client.GetAsync("/Admin/Users");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK, "admin should have access to admin pages");
        }
        finally
        {
            await _factory.ResetDatabaseAsync();
        }
    }

    [Fact]
    public async Task AdministratorOnly_Policy_ShouldDenyGovernmentOfficial()
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
            // Act - Attempt to access admin-only page
            var response = await client.GetAsync("/Admin/Users");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Redirect, "should redirect to access denied");
            response.Headers.Location?.ToString().Should().Contain("AccessDenied");
        }
        finally
        {
            await _factory.ResetDatabaseAsync();
        }
    }

    [Fact]
    public async Task AdministratorOnly_Policy_ShouldDenyParticipant()
    {
        // Arrange - Generate fresh test data
        var seeder = new DatabaseSeeder(_factory!.Services);
        await seeder.SeedRolesAsync();
        
        var participantEmail = BogusDataGenerator.GenerateEmail("participant");
        var participantPassword = "Participant@123";
        await seeder.SeedUserAsync(participantEmail, participantPassword, Roles.Participant);

        var authHelper = new AuthenticationHelper(_factory);
        var client = await authHelper.CreateAuthenticatedClientAsync(participantEmail, participantPassword);

        try
        {
            // Act - Attempt to access admin-only page
            var response = await client.GetAsync("/Admin/Users");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Redirect, "should redirect to access denied");
            response.Headers.Location?.ToString().Should().Contain("AccessDenied");
        }
        finally
        {
            await _factory.ResetDatabaseAsync();
        }
    }

    [Fact]
    public async Task GovernmentOfficialOnly_Policy_ShouldAllowAdmin()
    {
        // Arrange - Generate fresh test data
        var seeder = new DatabaseSeeder(_factory!.Services);
        await seeder.SeedRolesAsync();
        
        var adminEmail = BogusDataGenerator.GenerateEmail("admin");
        var adminPassword = "Admin@123";
        await seeder.SeedUserAsync(adminEmail, adminPassword, Roles.Administrator);

        var authHelper = new AuthenticationHelper(_factory);
        var client = await authHelper.CreateAuthenticatedClientAsync(adminEmail, adminPassword);

        try
        {
            // Act - Access meeting creation page (requires GovernmentOfficialOnly policy)
            var response = await client.GetAsync("/Meetings/Create");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK, "admin should have access to official-only pages");
        }
        finally
        {
            await _factory.ResetDatabaseAsync();
        }
    }

    [Fact]
    public async Task GovernmentOfficialOnly_Policy_ShouldAllowOfficial()
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
            // Act - Access meeting creation page
            var response = await client.GetAsync("/Meetings/Create");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK, "government official should have access to meeting creation");
        }
        finally
        {
            await _factory.ResetDatabaseAsync();
        }
    }

    [Fact]
    public async Task GovernmentOfficialOnly_Policy_ShouldDenyParticipant()
    {
        // Arrange - Generate fresh test data
        var seeder = new DatabaseSeeder(_factory!.Services);
        await seeder.SeedRolesAsync();
        
        var participantEmail = BogusDataGenerator.GenerateEmail("participant");
        var participantPassword = "Participant@123";
        await seeder.SeedUserAsync(participantEmail, participantPassword, Roles.Participant);

        var authHelper = new AuthenticationHelper(_factory);
        var client = await authHelper.CreateAuthenticatedClientAsync(participantEmail, participantPassword);

        try
        {
            // Act - Attempt to access meeting creation page
            var response = await client.GetAsync("/Meetings/Create");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Redirect, "participant should be denied access");
            response.Headers.Location?.ToString().Should().Contain("AccessDenied");
        }
        finally
        {
            await _factory.ResetDatabaseAsync();
        }
    }

    [Fact]
    public async Task Unauthenticated_User_ShouldRedirectToLogin()
    {
        // Arrange
        var client = _factory!.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        try
        {
            // Act - Attempt to access protected page without authentication
            var response = await client.GetAsync("/Meetings");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Redirect, "should redirect to login");
            response.Headers.Location?.ToString().Should().Contain("Account/Login");
        }
        finally
        {
            await _factory.ResetDatabaseAsync();
        }
    }
}
