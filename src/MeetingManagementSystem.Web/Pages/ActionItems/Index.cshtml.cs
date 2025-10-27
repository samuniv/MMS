using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Enums;
using MeetingManagementSystem.Core.Interfaces;
using System.Security.Claims;

namespace MeetingManagementSystem.Web.Pages.ActionItems;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IActionItemService _actionItemService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IActionItemService actionItemService,
        ILogger<IndexModel> logger)
    {
        _actionItemService = actionItemService;
        _logger = logger;
    }

    public IEnumerable<ActionItem> ActionItems { get; set; } = new List<ActionItem>();
    public IEnumerable<ActionItem> OverdueItems { get; set; } = new List<ActionItem>();
    public IEnumerable<ActionItem> PendingItems { get; set; } = new List<ActionItem>();
    public IEnumerable<ActionItem> InProgressItems { get; set; } = new List<ActionItem>();
    public IEnumerable<ActionItem> CompletedItems { get; set; } = new List<ActionItem>();

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            ActionItems = await _actionItemService.GetActionItemsByUserIdAsync(userId);

            // Get overdue items
            OverdueItems = ActionItems.Where(a => 
                a.DueDate.Date < DateTime.UtcNow.Date && 
                a.Status != ActionItemStatus.Completed).ToList();

            // Group by status
            PendingItems = ActionItems.Where(a => a.Status == ActionItemStatus.Pending).ToList();
            InProgressItems = ActionItems.Where(a => a.Status == ActionItemStatus.InProgress).ToList();
            CompletedItems = ActionItems.Where(a => a.Status == ActionItemStatus.Completed).ToList();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading action items");
            return Page();
        }
    }

    public async Task<IActionResult> OnPostCompleteAsync(int id)
    {
        try
        {
            await _actionItemService.CompleteActionItemAsync(id);
            TempData["SuccessMessage"] = "Action item marked as completed";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing action item {ActionItemId}", id);
            TempData["ErrorMessage"] = "Failed to complete action item";
        }

        return RedirectToPage();
    }
}
