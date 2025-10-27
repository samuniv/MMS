using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Enums;
using MeetingManagementSystem.Core.Interfaces;

namespace MeetingManagementSystem.Web.Pages.ActionItems;

[Authorize]
public class EditModel : PageModel
{
    private readonly IActionItemService _actionItemService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        IActionItemService actionItemService,
        ILogger<EditModel> logger)
    {
        _actionItemService = actionItemService;
        _logger = logger;
    }

    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public string Description { get; set; } = string.Empty;

    [BindProperty]
    public DateTime DueDate { get; set; }

    [BindProperty]
    public ActionItemStatus Status { get; set; }

    public ActionItem? ActionItem { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        ActionItem = await _actionItemService.GetActionItemByIdAsync(id);

        if (ActionItem == null)
        {
            return NotFound();
        }

        Id = ActionItem.Id;
        Description = ActionItem.Description;
        DueDate = ActionItem.DueDate;
        Status = ActionItem.Status;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ActionItem = await _actionItemService.GetActionItemByIdAsync(Id);
            return Page();
        }

        try
        {
            var updateDto = new UpdateActionItemDto
            {
                Description = Description,
                DueDate = DueDate,
                Status = Status
            };

            await _actionItemService.UpdateActionItemAsync(Id, updateDto);
            TempData["SuccessMessage"] = "Action item updated successfully";

            return RedirectToPage("/ActionItems/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating action item {ActionItemId}", Id);
            ModelState.AddModelError(string.Empty, "An error occurred while updating the action item");
            ActionItem = await _actionItemService.GetActionItemByIdAsync(Id);
            return Page();
        }
    }
}
