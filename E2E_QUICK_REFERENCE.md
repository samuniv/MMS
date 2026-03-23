# E2E Testing Quick Reference

## Quick Start

```powershell
# 1. First-time setup (installs Playwright browsers)
.\run-e2e-tests.ps1 -Action setup

# 2. Run all tests
.\run-e2e-tests.ps1 -Action run
```

## Common Commands

```powershell
# Run only Playwright browser tests
.\run-e2e-tests.ps1 -Action run-workflows

# Run only HTTP integration tests
.\run-e2e-tests.ps1 -Action run-integration

# Run only Chromium tests
.\run-e2e-tests.ps1 -Action run-chromium

# Run only Firefox tests
.\run-e2e-tests.ps1 -Action run-firefox

# Clean test artifacts
.\run-e2e-tests.ps1 -Action clean

# Verbose output
.\run-e2e-tests.ps1 -Action run -Verbose
```

## Manual Test Execution

```powershell
# Run all E2E tests
dotnet test tests/MeetingManagementSystem.E2ETests

# Run specific test class
dotnet test tests/MeetingManagementSystem.E2ETests --filter "FullyQualifiedName~AuthenticationWorkflowTests"

# Run with detailed output
dotnet test tests/MeetingManagementSystem.E2ETests --logger "console;verbosity=detailed"
```

## Project Structure

```
tests/MeetingManagementSystem.E2ETests/
├── Fixtures/              # Test infrastructure (containers, factories)
├── Helpers/               # Utilities (data generation, auth, forms)
├── PageObjects/           # Page Object Models for UI testing
├── Workflows/             # Playwright browser E2E tests
├── Integration/           # HTTP-level integration tests
├── appsettings.Testing.json
├── xunit.runner.json
└── README.md             # Comprehensive guide
```

## Writing New Tests

### Browser Workflow Test Template

```csharp
[Collection("E2E Collection")]
public class MyWorkflowTests : IAsyncLifetime
{
    private readonly PostgreSqlContainerFixture _dbFixture;
    private readonly PlaywrightFixture _playwrightFixture;
    private CustomWebApplicationFactory? _factory;
    private string _baseUrl = "";
    private bool _testFailed;

    // Constructor and lifecycle methods...

    [Theory]
    [InlineData("chromium")]
    [InlineData("firefox")]
    public async Task MyTest(string browserType)
    {
        // Arrange - Fresh test data
        var seeder = new DatabaseSeeder(_factory!.Services);
        await seeder.SeedRolesAsync();
        var email = BogusDataGenerator.GenerateEmail("test");
        var password = "Test@123";
        await seeder.SeedUserAsync(email, password, Roles.Participant);

        var browser = browserType == "chromium" 
            ? _playwrightFixture.ChromiumBrowser 
            : _playwrightFixture.FirefoxBrowser;
        var context = await _playwrightFixture.CreateContextAsync(browser, $"{nameof(MyTest)}_{browserType}");
        var page = await context.NewPageAsync();

        try
        {
            // Act - Test actions
            var loginPage = new LoginPage(page, _baseUrl);
            await loginPage.NavigateAsync();
            await loginPage.LoginAsync(email, password);

            // Assert
            (await loginPage.IsOnLoginPageAsync()).Should().BeFalse();
            
            _testFailed = false;
        }
        catch
        {
            _testFailed = true;
            throw;
        }
        finally
        {
            await _playwrightFixture.StopTracingAsync(context, $"{nameof(MyTest)}_{browserType}", _testFailed);
            await _factory!.ResetDatabaseAsync();
        }
    }
}
```

### HTTP Integration Test Template

```csharp
[Collection("E2E Collection")]
public class MyIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainerFixture _dbFixture;
    private CustomWebApplicationFactory? _factory;

    // Constructor and lifecycle methods...

    [Fact]
    public async Task MyTest()
    {
        // Arrange - Fresh test data
        var seeder = new DatabaseSeeder(_factory!.Services);
        await seeder.SeedRolesAsync();
        var email = BogusDataGenerator.GenerateEmail("test");
        var password = "Test@123";
        await seeder.SeedUserAsync(email, password, Roles.Participant);

        var authHelper = new AuthenticationHelper(_factory);
        var client = await authHelper.CreateAuthenticatedClientAsync(email, password);

        try
        {
            // Act
            var response = await client.GetAsync("/Meetings");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        }
        finally
        {
            await _factory.ResetDatabaseAsync();
        }
    }
}
```

## Test Data Generation

```csharp
// Generate user
var email = BogusDataGenerator.GenerateEmail("testuser");
var password = BogusDataGenerator.GenerateValidPassword();

// Generate meeting
var meeting = BogusDataGenerator.GenerateMeeting(organizerId, roomId);

// Generate room
var room = BogusDataGenerator.GenerateRoom();

// Edge cases
var longString = BogusDataGenerator.EdgeCases.VeryLongString(1000);
var specialChars = BogusDataGenerator.EdgeCases.StringWithSpecialCharacters();
```

## Debugging Failed Tests

### View Playwright Traces
```powershell
# Install Playwright CLI globally (one time)
dotnet tool install --global Microsoft.Playwright.CLI

# View trace
playwright show-trace tests/MeetingManagementSystem.E2ETests/test-results/{testname}-trace.zip
```

### Docker Troubleshooting
```powershell
# Check running containers
docker ps

# View container logs
docker logs <container-id>

# Stop all test containers
docker stop $(docker ps -a -q --filter name=mms-postgres-test)

# Clean up
docker system prune -a
```

## CI/CD

Tests run automatically on:
- Push to `main` or `develop`
- Pull requests
- Manual workflow dispatch

View results in GitHub Actions → E2E Tests workflow

## Key Features

- ✅ **Parallel Execution** - Tests run across collections and browsers simultaneously
- ✅ **Fresh Data** - Unique data per test, no shared state
- ✅ **Isolated Containers** - Each collection gets own PostgreSQL instance
- ✅ **Trace on Failure** - Screenshots and traces saved automatically
- ✅ **Fast Cleanup** - Respawn resets DB without container restart
- ✅ **Two Browsers** - Chromium and Firefox coverage

## Resources

- Full Documentation: `tests/MeetingManagementSystem.E2ETests/README.md`
- Playwright Docs: https://playwright.dev/dotnet/
- Testcontainers Docs: https://dotnet.testcontainers.org/
- FluentAssertions: https://fluentassertions.com/

## Tips

1. Always generate fresh test data with Bogus
2. Use Page Object Models for UI interactions
3. Clean up database in `finally` blocks
4. Mark tests with `[Collection("E2E Collection")]`
5. Test in both browsers with `[Theory]` and `[InlineData]`
6. Capture traces only on failure
7. Use meaningful test names
8. Assert with descriptive messages
