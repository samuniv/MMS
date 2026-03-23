namespace MeetingManagementSystem.E2ETests.PageObjects;

/// <summary>
/// Page Object Model for the Login page.
/// </summary>
public class LoginPage
{
    private readonly IPage _page;
    private readonly string _baseUrl;

    // Selectors
    private const string EmailInput = "input[name='Input.Email']";
    private const string PasswordInput = "input[name='Input.Password']";
    private const string RememberMeCheckbox = "input[name='Input.RememberMe']";
    private const string SubmitButton = "button[type='submit']";
    private const string ValidationSummary = ".validation-summary-errors";
    private const string ValidationError = ".field-validation-error";

    public LoginPage(IPage page, string baseUrl)
    {
        _page = page;
        _baseUrl = baseUrl;
    }

    public async Task NavigateAsync()
    {
        await _page.GotoAsync($"{_baseUrl}/Account/Login", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.Load);
        
        // Debug: Capture page state
        Console.WriteLine($"Current URL after navigation: {_page.Url}");
        Console.WriteLine($"Page title: {await _page.TitleAsync()}");
        var htmlContent = await _page.ContentAsync();
        Console.WriteLine($"Page HTML length: {htmlContent.Length} chars");
        Console.WriteLine($"Page HTML (first 500 chars): {htmlContent.Substring(0, Math.Min(500, htmlContent.Length))}");
        
        // Check if email input exists anywhere in DOM
        var emailInputCount = await _page.Locator(EmailInput).CountAsync();
        Console.WriteLine($"Email input count in DOM: {emailInputCount}");
        
        // Wait for the email input to be visible and ready
        await _page.WaitForSelectorAsync(EmailInput, new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 60000 });
    }

    public async Task FillEmailAsync(string email)
    {
        // Wait for email input to be ready before filling
        await _page.WaitForSelectorAsync(EmailInput, new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
        await _page.FillAsync(EmailInput, email);
    }

    public async Task FillPasswordAsync(string password)
    {
        await _page.FillAsync(PasswordInput, password);
    }

    public async Task CheckRememberMeAsync()
    {
        await _page.CheckAsync(RememberMeCheckbox);
    }

    public async Task ClickSubmitAsync()
    {
        await _page.ClickAsync(SubmitButton);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task LoginAsync(string email, string password, bool rememberMe = false)
    {
        await FillEmailAsync(email);
        await FillPasswordAsync(password);
        
        if (rememberMe)
        {
            await CheckRememberMeAsync();
        }
        
        await ClickSubmitAsync();
    }

    public async Task<bool> HasValidationErrorsAsync()
    {
        return await _page.Locator(ValidationSummary).IsVisibleAsync() ||
               await _page.Locator(ValidationError).IsVisibleAsync();
    }

    public async Task<string> GetValidationErrorTextAsync()
    {
        var summaryVisible = await _page.Locator(ValidationSummary).IsVisibleAsync();
        if (summaryVisible)
        {
            return await _page.Locator(ValidationSummary).TextContentAsync() ?? "";
        }

        var errorVisible = await _page.Locator(ValidationError).IsVisibleAsync();
        if (errorVisible)
        {
            return await _page.Locator(ValidationError).TextContentAsync() ?? "";
        }

        return "";
    }

    public async Task<string> GetCurrentUrlAsync()
    {
        return _page.Url;
    }

    public async Task<bool> IsOnLoginPageAsync()
    {
        return _page.Url.Contains("/Account/Login");
    }
}
