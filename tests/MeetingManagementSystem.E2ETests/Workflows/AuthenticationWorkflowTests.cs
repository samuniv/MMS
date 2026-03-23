using MeetingManagementSystem.E2ETests.PageObjects;

namespace MeetingManagementSystem.E2ETests.Workflows;

/// <summary>
/// E2E tests for authentication workflows using Playwright.
/// Tests run in parallel across Chromium and Firefox browsers.
/// </summary>
[Collection("E2E Collection")]
public class AuthenticationWorkflowTests : IAsyncLifetime
{
    private readonly PostgreSqlContainerFixture _dbFixture;
    private readonly PlaywrightFixture _playwrightFixture;
    private CustomWebApplicationFactory? _factory;
    private string _baseUrl = "";
    private bool _testFailed;

    public AuthenticationWorkflowTests(
        PostgreSqlContainerFixture dbFixture,
        PlaywrightFixture playwrightFixture)
    {
        _dbFixture = dbFixture;
        _playwrightFixture = playwrightFixture;
    }

    public async Task InitializeAsync()
    {
        // Create factory with container connection string
        _factory = new CustomWebApplicationFactory(_dbFixture.ConnectionString);
        await _factory.InitializeDatabaseAsync();
        
        // Get the server URL
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
    public async Task Login_WithValidCredentials_ShouldSucceed(string browserType)
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
        
        var context = await _playwrightFixture.CreateContextAsync(browser, $"{nameof(Login_WithValidCredentials_ShouldSucceed)}_{browserType}");
        var page = await context.NewPageAsync();

        try
        {
            // Act
            var loginPage = new LoginPage(page, _baseUrl);
            await loginPage.NavigateAsync();
            await loginPage.LoginAsync(email, password);

            // Assert
            (await loginPage.IsOnLoginPageAsync()).Should().BeFalse("user should be redirected after successful login");
            page.Url.Should().NotContain("/Account/Login");
            
            _testFailed = false;
        }
        catch
        {
            _testFailed = true;
            throw;
        }
        finally
        {
            await _playwrightFixture.StopTracingAsync(context, $"{nameof(Login_WithValidCredentials_ShouldSucceed)}_{browserType}", _testFailed);
            await _factory!.ResetDatabaseAsync();
        }
    }

    [Theory]
    [InlineData("chromium")]
    [InlineData("firefox")]
    public async Task Login_WithInvalidCredentials_ShouldShowError(string browserType)
    {
        // Arrange
        var email = BogusDataGenerator.GenerateEmail("invalid");
        var password = "WrongPassword123!";

        var browser = browserType == "chromium" 
            ? _playwrightFixture.ChromiumBrowser 
            : _playwrightFixture.FirefoxBrowser;
        
        var context = await _playwrightFixture.CreateContextAsync(browser, $"{nameof(Login_WithInvalidCredentials_ShouldShowError)}_{browserType}");
        var page = await context.NewPageAsync();

        try
        {
            // Act
            var loginPage = new LoginPage(page, _baseUrl);
            await loginPage.NavigateAsync();
            await loginPage.LoginAsync(email, password);

            // Assert
            (await loginPage.IsOnLoginPageAsync()).Should().BeTrue("user should remain on login page");
            (await loginPage.HasValidationErrorsAsync()).Should().BeTrue("validation error should be displayed");
            
            _testFailed = false;
        }
        catch
        {
            _testFailed = true;
            throw;
        }
        finally
        {
            await _playwrightFixture.StopTracingAsync(context, $"{nameof(Login_WithInvalidCredentials_ShouldShowError)}_{browserType}", _testFailed);
            await _factory!.ResetDatabaseAsync();
        }
    }

    [Theory]
    [InlineData("chromium")]
    [InlineData("firefox")]
    public async Task Login_WithMultipleFailedAttempts_ShouldLockAccount(string browserType)
    {
        // Arrange - Generate fresh test data
        var seeder = new DatabaseSeeder(_factory!.Services);
        await seeder.SeedRolesAsync();
        
        var email = BogusDataGenerator.GenerateEmail("locktest");
        var correctPassword = "Correct@123";
        var wrongPassword = "Wrong@123";
        await seeder.SeedUserAsync(email, correctPassword, Roles.Participant);

        var browser = browserType == "chromium" 
            ? _playwrightFixture.ChromiumBrowser 
            : _playwrightFixture.FirefoxBrowser;
        
        var context = await _playwrightFixture.CreateContextAsync(browser, $"{nameof(Login_WithMultipleFailedAttempts_ShouldLockAccount)}_{browserType}");
        var page = await context.NewPageAsync();

        try
        {
            var loginPage = new LoginPage(page, _baseUrl);
            await loginPage.NavigateAsync();

            // Act - Attempt login 5 times with wrong password
            for (int i = 0; i < 5; i++)
            {
                await loginPage.LoginAsync(email, wrongPassword);
                await Task.Delay(500); // Small delay between attempts
                await loginPage.NavigateAsync(); // Navigate back to login page
            }

            // Attempt with correct password (should be locked out)
            await loginPage.LoginAsync(email, correctPassword);

            // Assert
            (await loginPage.IsOnLoginPageAsync()).Should().BeTrue("user should remain on login page due to lockout");
            var errorText = await loginPage.GetValidationErrorTextAsync();
            errorText.Should().Contain("locked", "error message should indicate account is locked");
            
            _testFailed = false;
        }
        catch
        {
            _testFailed = true;
            throw;
        }
        finally
        {
            await _playwrightFixture.StopTracingAsync(context, $"{nameof(Login_WithMultipleFailedAttempts_ShouldLockAccount)}_{browserType}", _testFailed);
            await _factory!.ResetDatabaseAsync();
        }
    }

    [Theory]
    [InlineData("chromium")]
    [InlineData("firefox")]
    public async Task Logout_WhenAuthenticated_ShouldRedirectToLogin(string browserType)
    {
        // Arrange - Generate fresh test data
        var seeder = new DatabaseSeeder(_factory!.Services);
        await seeder.SeedRolesAsync();
        
        var email = BogusDataGenerator.GenerateEmail("logouttest");
        var password = "Test@123";
        await seeder.SeedUserAsync(email, password, Roles.Participant);

        var browser = browserType == "chromium" 
            ? _playwrightFixture.ChromiumBrowser 
            : _playwrightFixture.FirefoxBrowser;
        
        var context = await _playwrightFixture.CreateContextAsync(browser, $"{nameof(Logout_WhenAuthenticated_ShouldRedirectToLogin)}_{browserType}");
        var page = await context.NewPageAsync();

        try
        {
            // Act - Login first
            var loginPage = new LoginPage(page, _baseUrl);
            await loginPage.NavigateAsync();
            await loginPage.LoginAsync(email, password);

            // Navigate to logout
            await page.GotoAsync($"{_baseUrl}/Account/Logout");
            
            // Click logout button if present
            var logoutButton = await page.QuerySelectorAsync("button[type='submit']");
            if (logoutButton != null)
            {
                await logoutButton.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }

            // Assert
            page.Url.Should().Contain("/Account/Login", "user should be redirected to login page after logout");
            
            _testFailed = false;
        }
        catch
        {
            _testFailed = true;
            throw;
        }
        finally
        {
            await _playwrightFixture.StopTracingAsync(context, $"{nameof(Logout_WhenAuthenticated_ShouldRedirectToLogin)}_{browserType}", _testFailed);
            await _factory!.ResetDatabaseAsync();
        }
    }
}
