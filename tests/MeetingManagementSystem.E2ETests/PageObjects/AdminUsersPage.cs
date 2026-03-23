namespace MeetingManagementSystem.E2ETests.PageObjects;

/// <summary>
/// Page Object Model for the Admin Users page.
/// </summary>
public class AdminUsersPage
{
    private readonly IPage _page;
    private readonly string _baseUrl;

    // Selectors
    private const string CreateUserButton = "a[href*='/Admin/Users/Create']";
    private const string UserTable = "table.users-table";
    private const string UserRow = "tr[data-user-id]";
    private const string EditButton = "a[href*='/Admin/Users/Edit']";
    private const string DeleteButton = "button[data-action='delete']";
    private const string SearchInput = "input[name='search']";
    private const string RoleFilter = "select[name='role']";

    public AdminUsersPage(IPage page, string baseUrl)
    {
        _page = page;
        _baseUrl = baseUrl;
    }

    public async Task NavigateAsync()
    {
        await _page.GotoAsync($"{_baseUrl}/Admin/Users");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task ClickCreateUserAsync()
    {
        await _page.ClickAsync(CreateUserButton);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task<bool> IsUserTableVisibleAsync()
    {
        return await _page.Locator(UserTable).IsVisibleAsync();
    }

    public async Task<int> GetUserCountAsync()
    {
        var rows = await _page.Locator(UserRow).AllAsync();
        return rows.Count;
    }

    public async Task SearchUsersAsync(string searchTerm)
    {
        await _page.FillAsync(SearchInput, searchTerm);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task FilterByRoleAsync(string role)
    {
        await _page.SelectOptionAsync(RoleFilter, role);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task<bool> IsUserVisibleAsync(string email)
    {
        var userCell = _page.Locator($"td:has-text('{email}')");
        return await userCell.IsVisibleAsync();
    }

    public async Task ClickEditUserAsync(string userId)
    {
        var editButton = _page.Locator($"tr[data-user-id='{userId}'] {EditButton}");
        await editButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task ClickDeleteUserAsync(string userId)
    {
        var deleteButton = _page.Locator($"tr[data-user-id='{userId}'] {DeleteButton}");
        await deleteButton.ClickAsync();
    }

    public async Task<bool> IsOnAdminUsersPageAsync()
    {
        return _page.Url.Contains("/Admin/Users");
    }

    public async Task<bool> HasAccessDeniedAsync()
    {
        return _page.Url.Contains("/Account/AccessDenied") ||
               await _page.Locator("text=Access Denied").IsVisibleAsync();
    }
}
