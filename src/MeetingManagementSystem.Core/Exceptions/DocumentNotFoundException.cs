namespace MeetingManagementSystem.Core.Exceptions;

public class DocumentNotFoundException : Exception
{
    public DocumentNotFoundException(int documentId) 
        : base($"Document with ID {documentId} was not found.")
    {
    }
}
