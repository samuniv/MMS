using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MeetingManagementSystem.Infrastructure.Repositories;

public class DocumentRepository : Repository<MeetingDocument>, IDocumentRepository
{
    public DocumentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<MeetingDocument>> GetByMeetingIdAsync(int meetingId)
    {
        return await _context.MeetingDocuments
            .Include(d => d.UploadedBy)
            .Where(d => d.MeetingId == meetingId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
    }

    public async Task<MeetingDocument?> GetByIdWithDetailsAsync(int documentId)
    {
        return await _context.MeetingDocuments
            .Include(d => d.Meeting)
            .Include(d => d.UploadedBy)
            .FirstOrDefaultAsync(d => d.Id == documentId);
    }
}
