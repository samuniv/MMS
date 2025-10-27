using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace MeetingManagementSystem.Web.Pages.Documents;

[Authorize]
public class UploadModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly IMeetingService _meetingService;
    private readonly ILogger<UploadModel> _logger;

    public UploadModel(
        IDocumentService documentService,
        IMeetingService meetingService,
        ILogger<UploadModel> logger)
    {
        _documentService = documentService;
        _meetingService = meetingService;
        _logger = logger;
    }

    [BindProperty]
    public int MeetingId { get; set; }

    [BindProperty]
    public IFormFile? UploadedFile { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int meetingId)
    {
        MeetingId = meetingId;

        // Verify meeting exists
        var meeting = await _meetingService.GetMeetingByIdAsync(meetingId);
        if (meeting == null)
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (UploadedFile == null)
        {
            ErrorMessage = "Please select a file to upload.";
            return Page();
        }

        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            var document = await _documentService.UploadDocumentAsync(MeetingId, UploadedFile, userId);
            
            TempData["SuccessMessage"] = $"Document '{document.FileName}' uploaded successfully.";
            return RedirectToPage("/Meetings/Details", new { id = MeetingId });
        }
        catch (MeetingNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidFileException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document for meeting {MeetingId}", MeetingId);
            ErrorMessage = "An error occurred while uploading the document. Please try again.";
            return Page();
        }
    }
}
