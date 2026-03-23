namespace MeetingManagementSystem.E2ETests.PageObjects;

/// <summary>
/// Page Object Model for the Room Calendar page.
/// </summary>
public class RoomCalendarPage
{
    private readonly IPage _page;
    private readonly string _baseUrl;

    // Selectors
    private const string RoomSelect = "select[name='roomId']";
    private const string DatePicker = "input[name='date']";
    private const string CalendarView = ".calendar-view";
    private const string TimeSlot = ".time-slot";
    private const string AvailableSlot = ".time-slot.available";
    private const string BookedSlot = ".time-slot.booked";
    private const string BookButton = "button[data-action='book']";

    public RoomCalendarPage(IPage page, string baseUrl)
    {
        _page = page;
        _baseUrl = baseUrl;
    }

    public async Task NavigateAsync()
    {
        await _page.GotoAsync($"{_baseUrl}/Rooms/Calendar");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task SelectRoomAsync(string roomId)
    {
        await _page.SelectOptionAsync(RoomSelect, roomId);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task SelectDateAsync(string date)
    {
        await _page.FillAsync(DatePicker, date);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task<bool> IsCalendarVisibleAsync()
    {
        return await _page.Locator(CalendarView).IsVisibleAsync();
    }

    public async Task<int> GetAvailableTimeSlotsCountAsync()
    {
        var slots = await _page.Locator(AvailableSlot).AllAsync();
        return slots.Count;
    }

    public async Task<int> GetBookedTimeSlotsCountAsync()
    {
        var slots = await _page.Locator(BookedSlot).AllAsync();
        return slots.Count;
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

    public async Task ClickFirstAvailableSlotAsync()
    {
        var firstAvailable = _page.Locator(AvailableSlot).First;
        await firstAvailable.ClickAsync();
    }

    public async Task<bool> IsTimeSlotAvailableAsync(string timeSlot)
    {
        var slot = _page.Locator($"{AvailableSlot}[data-time='{timeSlot}']");
        return await slot.IsVisibleAsync();
    }

    public async Task<bool> IsOnCalendarPageAsync()
    {
        return _page.Url.Contains("/Rooms/Calendar");
    }
}
