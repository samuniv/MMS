using MeetingManagementSystem.Core.Entities;
using Microsoft.AspNetCore.Http;

namespace MeetingManagementSystem.Core.Interfaces;

public interface IDocumentService
{
    Task<MeetingDocument> UploadDocumentAsync(int meetingId, IFormFile file, int uploadedById);
    Task<MeetingDocument?> GetDocumentByIdAsync(int documentId);
    Task<IEnumerable<MeetingDocument>> GetMeetingDocumentsAsync(int meetingId);
    Task<bool> DeleteDocumentAsync(int documentId, int userId);
    Task<(Stream FileStream, string ContentType, string FileName)> GetDocumentStreamAsync(int documentId);
    Task<bool> ValidateFileAsync(IFormFile file);
}
