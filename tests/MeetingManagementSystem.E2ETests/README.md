# End-to-End Testing Guide

This document provides comprehensive information about the E2E testing infrastructure for the Meeting Management System.

## Overview

The E2E testing suite uses:
- **Playwright for .NET** - Browser automation for UI testing (Chromium & Firefox)
- **Testcontainers** - Isolated PostgreSQL containers per test collection
- **WebApplicationFactory** - HTTP-level integration testing
- **AngleSharp** - HTML parsing and form handling
- **Respawn** - Fast database cleanup between tests
- **Bogus** - Realistic test data generation
- **xUnit** - Test framework with parallel execution support

## Test Architecture

### Test Structure

```
tests/MeetingManagementSystem.E2ETests/
├── Fixtures/                          # Test infrastructure setup
│   ├── PostgreSqlContainerFixture.cs  # PostgreSQL container per collection
│   ├── CustomWebApplicationFactory.cs # Test application factory
│   ├── PlaywrightFixture.cs           # Browser lifecycle management
│   └── E2ETestCollection.cs           # Collection definition
├── Helpers/                           # Test utilities
│   ├── BogusDataGenerator.cs          # Test data generation
│   ├── AuthenticationHelper.cs        # Login/logout utilities
│   ├── FormHelper.cs                  # CSRF token extraction
│   └── DatabaseSeeder.cs              # Database seeding
├── PageObjects/                       # Page Object Models
│   ├── LoginPage.cs
│   ├── MeetingCreatePage.cs
│   ├── RoomCalendarPage.cs
│   └── AdminUsersPage.cs
├── Workflows/                         # Browser-based E2E tests
│   ├── AuthenticationWorkflowTests.cs
│   ├── MeetingLifecycleTests.cs
│   └── RoomBookingTests.cs
└── Integration/                       # HTTP integration tests
    ├── AuthorizationTests.cs
    ├── CsrfProtectionTests.cs
    └── ConcurrentOperationsTests.cs
```

## Running Tests

### Prerequisites

```powershell
# Install .NET 9.0 SDK
# Install Docker Desktop (for Testcontainers)

# Restore packages
dotnet restore

# Build the test project
dotnet build tests/MeetingManagementSystem.E2ETests
```

### Install Playwright Browsers

```powershell
cd tests/MeetingManagementSystem.E2ETests
pwsh bin/Debug/net9.0/playwright.ps1 install chromium firefox
```

### Run All Tests

```powershell
# Run all E2E tests
dotnet test tests/MeetingManagementSystem.E2ETests

# Run with verbose output
dotnet test tests/MeetingManagementSystem.E2ETests --logger "console;verbosity=detailed"
```

### Run Specific Test Categories

```powershell
# Run only Playwright workflow tests
dotnet test tests/MeetingManagementSystem.E2ETests --filter "FullyQualifiedName~Workflows"

# Run only HTTP integration tests
dotnet test tests/MeetingManagementSystem.E2ETests --filter "FullyQualifiedName~Integration"

# Run only Chromium tests
dotnet test tests/MeetingManagementSystem.E2ETests --filter "BrowserType=chromium"

# Run only Firefox tests
dotnet test tests/MeetingManagementSystem.E2ETests --filter "BrowserType=firefox"
```

### Run Specific Test Class

```powershell
# Run authentication workflow tests
dotnet test tests/MeetingManagementSystem.E2ETests --filter "FullyQualifiedName~AuthenticationWorkflowTests"

# Run authorization tests
dotnet test tests/MeetingManagementSystem.E2ETests --filter "FullyQualifiedName~AuthorizationTests"
```

## Test Configuration

### Parallel Execution

Tests are configured to run in parallel (see `xunit.runner.json`):
- **parallelizeAssembly**: true
- **parallelizeTestCollections**: true
- **maxParallelThreads**: 4

Each test collection gets its own isolated PostgreSQL Testcontainer.

### Browser Configuration

Playwright tests run against both Chromium and Firefox in parallel using xUnit's `[Theory]` attribute:

```csharp
[Theory]
[InlineData("chromium")]
[InlineData("firefox")]
public async Task MyTest(string browserType)
{
    // Test runs twice - once for each browser
}
```

### Tracing and Screenshots

Playwright captures traces and screenshots **only on test failure** to minimize disk usage:
- Traces: `test-results/{testname}-trace.zip`
- View traces: `playwright show-trace test-results/{testname}-trace.zip`

## Test Data Strategy

### Fresh Data Per Test

All tests generate **fresh data using Bogus** for maximum isolation:

```csharp
// Generate unique test user
var email = BogusDataGenerator.GenerateEmail("testuser");
var password = "Test@123";
var user = await seeder.SeedUserAsync(email, password, Roles.Participant);

// Generate unique meeting
var meeting = BogusDataGenerator.GenerateMeeting(organizerId, roomId);
```

### Database Cleanup

Each test collection uses **Respawn** to reset the database after tests:

```csharp
await _factory.ResetDatabaseAsync();
```

## Writing New Tests

### 1. Browser-Based Workflow Test

```csharp
[Collection("E2E Collection")]
public class MyWorkflowTests : IAsyncLifetime
{
    private readonly PostgreSqlContainerFixture _dbFixture;
    private readonly PlaywrightFixture _playwrightFixture;
    private CustomWebApplicationFactory? _factory;
    private string _baseUrl = "";
    private bool _testFailed;

    public MyWorkflowTests(
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
    public async Task MyTest(string browserType)
    {
        // Arrange - Generate fresh test data
        var seeder = new DatabaseSeeder(_factory!.Services);
        await seeder.SeedRolesAsync();
        
        var email = BogusDataGenerator.GenerateEmail("testuser");
        var password = "Test@123";
        await seeder.SeedUserAsync(email, password, Roles.Participant);

        var browser = browserType == "chromium" 
            ? _playwrightFixture.ChromiumBrowser 
            : _playwrightFixture.FirefoxBrowser;
        
        var context = await _playwrightFixture.CreateContextAsync(browser, $"{nameof(MyTest)}_{browserType}");
        var page = await context.NewPageAsync();

        try
        {
            // Act - Perform test actions
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

### 2. HTTP Integration Test

```csharp
[Collection("E2E Collection")]
public class MyIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainerFixture _dbFixture;
    private CustomWebApplicationFactory? _factory;

    public MyIntegrationTests(PostgreSqlContainerFixture dbFixture)
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
    public async Task MyTest()
    {
        // Arrange - Generate fresh test data
        var seeder = new DatabaseSeeder(_factory!.Services);
        await seeder.SeedRolesAsync();
        
        var email = BogusDataGenerator.GenerateEmail("testuser");
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

## CI/CD Integration

### GitHub Actions

The E2E tests run automatically on:
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop`
- Manual workflow dispatch

See `.github/workflows/e2e-tests.yml` for configuration.

### Test Results

- Test results: Uploaded as TRX artifacts
- Playwright traces: Uploaded only on failure
- Test summary: Published to GitHub Actions summary

## Troubleshooting

### Playwright Browser Installation Issues

```powershell
# Manually install browsers with dependencies
pwsh tests/MeetingManagementSystem.E2ETests/bin/Debug/net9.0/playwright.ps1 install --with-deps chromium firefox
```

### Testcontainers Docker Issues

```powershell
# Ensure Docker Desktop is running
# Check Docker connection
docker ps

# Clean up old containers
docker system prune -a
```

### Database Connection Issues

```powershell
# Check Testcontainer logs
docker logs <container-id>

# Verify PostgreSQL is accessible
docker exec -it <container-id> psql -U postgres -d meetingmanagement_test
```

### Test Timeout Issues

If tests timeout, increase the timeout in test attributes:

```csharp
[Fact(Timeout = 60000)] // 60 seconds
public async Task MyLongRunningTest()
{
    // ...
}
```

## Best Practices

1. **Always generate fresh data** - Use BogusDataGenerator for each test
2. **Clean up after tests** - Use `ResetDatabaseAsync()` in finally blocks
3. **Capture traces only on failure** - Saves disk space and CI time
4. **Use Page Object Models** - Encapsulate page interactions
5. **Test in both browsers** - Use `[Theory]` with browser parameters
6. **Isolate test collections** - Each collection gets its own database container
7. **Handle async properly** - Use `IAsyncLifetime` for setup/teardown
8. **Assert meaningful errors** - Use descriptive failure messages

## Performance Considerations

- **Parallel execution**: Tests run in parallel across collections and browsers
- **Testcontainers**: New container per collection provides isolation but adds startup time
- **Respawn**: Fast database cleanup without recreating containers
- **Bogus**: Fast test data generation
- **Playwright headless**: Faster than headed mode

## Test Coverage

### Covered Workflows

- ✅ Authentication (login, logout, lockout)
- ✅ Meeting lifecycle (create, invite, respond)
- ✅ Room booking and conflicts
- ✅ Authorization policies (all 7 policies)
- ✅ CSRF protection
- ✅ Concurrent operations

### Future Test Ideas

- Meeting minutes recording and editing
- Action item creation and tracking
- Document upload and download
- Email notification verification (via MailHog API)
- Rate limiting enforcement
- Report generation
- Audit log verification
- Session timeout handling

## Resources

- [Playwright for .NET Documentation](https://playwright.dev/dotnet/)
- [Testcontainers for .NET](https://dotnet.testcontainers.org/)
- [Bogus Documentation](https://github.com/bchavez/Bogus)
- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions](https://fluentassertions.com/)
