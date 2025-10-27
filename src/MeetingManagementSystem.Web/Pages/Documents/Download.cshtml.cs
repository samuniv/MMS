using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace MeetingManagementSystem.Web.Pages.Documents;

[Authorize]
public class DownloadModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly IMeetingService _meetingService;
    private readonly ILogger<DownloadModel> _logger;

    public DownloadModel(
        IDocumentService documentService,
        IMeetingService meetingService,
        ILogger<DownloadModel> logger)
    {
        _documentService = documentService;
        _meetingService = meetingService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            // Get document details
            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null)
            {
                return NotFound();
            }

            // Verify user has access to the meeting
            var meeting = await _meetingService.GetMeetingByIdAsync(document.MeetingId);
            if (meeting == null)
            {
                return NotFound();
            }

            // Check authorization: user must be organizer, participant, or admin
            var isOrganizer = meeting.OrganizerId == userId;
            var isAdmin = User.IsInRole("Administrator");
            var isParticipant = meeting.Participants?.Any(p => p.UserId == userId) ?? false;

            if (!isOrganizer && !isAdmin && !isParticipant)
            {
                _logger.LogWarning("Unauthorized document access attempt by user {UserId} for document {DocumentId}", 
                    userId, id);
                return Forbid();
            }

            // Get file stream and return
            var (fileStream, contentType, fileName) = await _documentService.GetDocumentStreamAsync(id);
            
            _logger.LogInformation("Document {DocumentId} downloaded by user {UserId}", id, userId);
            
            return File(fileStream, contentType, fileName);
        }
        catch (DocumentNotFoundException)
        {
            return NotFound();
        }
        catch (FileNotFoundException)
        {
            _logger.LogError("Physical file not found for document {DocumentId}", id);
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {DocumentId}", id);
            return StatusCode(500);
        }
    }
}
