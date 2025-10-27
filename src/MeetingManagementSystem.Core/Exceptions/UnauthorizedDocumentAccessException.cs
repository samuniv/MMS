namespace MeetingManagementSystem.Core.Exceptions;

public class UnauthorizedDocumentAccessException : Exception
{
    public UnauthorizedDocumentAccessException(int userId, int documentId)
        : base($"User {userId} is not authorized to access document {documentId}.")
    {
    }
}
