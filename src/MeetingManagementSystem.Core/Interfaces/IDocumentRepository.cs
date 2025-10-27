using MeetingManagementSystem.Core.Entities;

namespace MeetingManagementSystem.Core.Interfaces;

public interface IDocumentRepository : IRepository<MeetingDocument>
{
    Task<IEnumerable<MeetingDocument>> GetByMeetingIdAsync(int meetingId);
    Task<MeetingDocument?> GetByIdWithDetailsAsync(int documentId);
}
