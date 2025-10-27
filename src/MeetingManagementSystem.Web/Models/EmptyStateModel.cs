namespace MeetingManagementSystem.Web.Models;

public class EmptyStateModel
{
    public string Icon { get; set; } = "fas fa-inbox";
    public string Title { get; set; } = "No items found";
    public string Message { get; set; } = "There are no items to display.";
    public string? ActionText { get; set; }
    public string? ActionUrl { get; set; }
}
