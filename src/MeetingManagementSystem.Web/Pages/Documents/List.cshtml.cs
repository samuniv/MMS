using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace MeetingManagementSystem.Web.Pages.Documents;

[Authorize]
public class ListModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly IMeetingService _meetingService;
    private readonly ILogger<ListModel> _logger;

    public ListModel(
        IDocumentService documentService,
        IMeetingService meetingService,
        ILogger<ListModel> logger)
    {
        _documentService = documentService;
        _meetingService = meetingService;
        _logger = logger;
    }

    public IEnumerable<MeetingDocument> Documents { get; set; } = new List<MeetingDocument>();
    public Meeting? Meeting { get; set; }
    public bool CanManageDocuments { get; set; }

    public async Task<IActionResult> OnGetAsync(int meetingId)
    {
        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            Meeting = await _meetingService.GetMeetingByIdAsync(meetingId);
            if (Meeting == null)
            {
                return NotFound();
            }

            // Check if user has access to view documents
            var isOrganizer = Meeting.OrganizerId == userId;
            var isAdmin = User.IsInRole("Administrator");
            var isParticipant = Meeting.Participants?.Any(p => p.UserId == userId) ?? false;

            if (!isOrganizer && !isAdmin && !isParticipant)
            {
                return Forbid();
            }

            CanManageDocuments = isOrganizer || isAdmin;
            Documents = await _documentService.GetMeetingDocumentsAsync(meetingId);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading documents for meeting {MeetingId}", meetingId);
            return NotFound();
        }
    }
}
