namespace MeetingManagementSystem.E2ETests.PageObjects;

/// <summary>
/// Page Object Model for the Create Meeting page.
/// </summary>
public class MeetingCreatePage
{
    private readonly IPage _page;
    private readonly string _baseUrl;

    // Selectors
    private const string TitleInput = "input[name='Input.Title']";
    private const string DescriptionTextarea = "textarea[name='Input.Description']";
    private const string StartTimeInput = "input[name='Input.StartTime']";
    private const string EndTimeInput = "input[name='Input.EndTime']";
    private const string LocationInput = "input[name='Input.Location']";
    private const string RoomSelect = "select[name='Input.MeetingRoomId']";
    private const string AgendaTextarea = "textarea[name='Input.Agenda']";
    private const string ParticipantsSelect = "select[name='Input.ParticipantIds']";
    private const string SubmitButton = "button[type='submit']";
    private const string ValidationSummary = ".validation-summary-errors";

    public MeetingCreatePage(IPage page, string baseUrl)
    {
        _page = page;
        _baseUrl = baseUrl;
    }

    public async Task NavigateAsync()
    {
        await _page.GotoAsync($"{_baseUrl}/Meetings/Create");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task FillTitleAsync(string title)
    {
        await _page.FillAsync(TitleInput, title);
    }

    public async Task FillDescriptionAsync(string description)
    {
        await _page.FillAsync(DescriptionTextarea, description);
    }

    public async Task FillStartTimeAsync(string startTime)
    {
        await _page.FillAsync(StartTimeInput, startTime);
    }

    public async Task FillEndTimeAsync(string endTime)
    {
        await _page.FillAsync(EndTimeInput, endTime);
    }

    public async Task FillLocationAsync(string location)
    {
        await _page.FillAsync(LocationInput, location);
    }

    public async Task SelectRoomAsync(string roomId)
    {
        await _page.SelectOptionAsync(RoomSelect, roomId);
    }

    public async Task FillAgendaAsync(string agenda)
    {
        await _page.FillAsync(AgendaTextarea, agenda);
    }

    public async Task SelectParticipantsAsync(string[] participantIds)
    {
        await _page.SelectOptionAsync(ParticipantsSelect, participantIds);
    }

    public async Task ClickSubmitAsync()
    {
        await _page.ClickAsync(SubmitButton);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task CreateMeetingAsync(
        string title,
        string description,
        string startTime,
        string endTime,
        string? location = null,
        string? roomId = null,
        string? agenda = null,
        string[]? participantIds = null)
    {
        await FillTitleAsync(title);
        await FillDescriptionAsync(description);
        await FillStartTimeAsync(startTime);
        await FillEndTimeAsync(endTime);

        if (!string.IsNullOrEmpty(location))
        {
            await FillLocationAsync(location);
        }

        if (!string.IsNullOrEmpty(roomId))
        {
            await SelectRoomAsync(roomId);
        }

        if (!string.IsNullOrEmpty(agenda))
        {
            await FillAgendaAsync(agenda);
        }

        if (participantIds != null && participantIds.Length > 0)
        {
            await SelectParticipantsAsync(participantIds);
        }

        await ClickSubmitAsync();
    }

    public async Task<bool> HasValidationErrorsAsync()
    {
        return await _page.Locator(ValidationSummary).IsVisibleAsync();
    }

    public async Task<string> GetValidationErrorTextAsync()
    {
        if (await HasValidationErrorsAsync())
        {
            return await _page.Locator(ValidationSummary).TextContentAsync() ?? "";
        }
        return "";
    }

    public async Task<List<string>> GetAvailableRoomsAsync()
    {
        var options = await _page.Locator($"{RoomSelect} option").AllAsync();
        var rooms = new List<string>();
        
        foreach (var option in options)
        {
            var value = await option.GetAttributeAsync("value");
            if (!string.IsNullOrEmpty(value))
            {
                rooms.Add(value);
            }
        }
        
        return rooms;
    }

    public async Task<bool> IsOnCreatePageAsync()
    {
        return _page.Url.Contains("/Meetings/Create");
    }
}
