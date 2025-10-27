using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace MeetingManagementSystem.Web.Pages.Documents;

[Authorize]
public class DeleteModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly IMeetingService _meetingService;
    private readonly ILogger<DeleteModel> _logger;

    public DeleteModel(
        IDocumentService documentService,
        IMeetingService meetingService,
        ILogger<DeleteModel> logger)
    {
        _documentService = documentService;
        _meetingService = meetingService;
        _logger = logger;
    }

    public string FileName { get; set; } = string.Empty;
    public int MeetingId { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null)
            {
                return NotFound();
            }

            // Verify user has permission to delete (organizer or admin)
            var meeting = await _meetingService.GetMeetingByIdAsync(document.MeetingId);
            if (meeting == null)
            {
                return NotFound();
            }

            var isOrganizer = meeting.OrganizerId == userId;
            var isAdmin = User.IsInRole("Administrator");

            if (!isOrganizer && !isAdmin)
            {
                return Forbid();
            }

            FileName = document.FileName;
            MeetingId = document.MeetingId;

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading delete page for document {DocumentId}", id);
            return NotFound();
        }
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null)
            {
                return NotFound();
            }

            // Verify user has permission to delete
            var meeting = await _meetingService.GetMeetingByIdAsync(document.MeetingId);
            if (meeting == null)
            {
                return NotFound();
            }

            var isOrganizer = meeting.OrganizerId == userId;
            var isAdmin = User.IsInRole("Administrator");

            if (!isOrganizer && !isAdmin)
            {
                return Forbid();
            }

            var meetingId = document.MeetingId;
            await _documentService.DeleteDocumentAsync(id, userId);
            
            TempData["SuccessMessage"] = "Document deleted successfully.";
            return RedirectToPage("/Meetings/Details", new { id = meetingId });
        }
        catch (DocumentNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the document.";
            return RedirectToPage("/Meetings/Details", new { id = MeetingId });
        }
    }
}
