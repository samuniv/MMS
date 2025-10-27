using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Exceptions;
using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Infrastructure.Services;

namespace MeetingManagementSystem.Tests.Services;

public class DocumentServiceTests : IDisposable
{
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<IMeetingRepository> _meetingRepositoryMock;
    private readonly Mock<ILogger<DocumentService>> _loggerMock;
    private readonly DocumentService _documentService;
    private readonly string _testUploadPath;

    public DocumentServiceTests()
    {
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _meetingRepositoryMock = new Mock<IMeetingRepository>();
        _loggerMock = new Mock<ILogger<DocumentService>>();

        _testUploadPath = Path.Combine(Path.GetTempPath(), "test_uploads_" + Guid.NewGuid());
        Directory.CreateDirectory(_testUploadPath);

        var settings = Options.Create(new DocumentSettings
        {
            MaxDocumentSizeMB = 10,
            AllowedFileTypes = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx" },
            UploadPath = _testUploadPath
        });

        _documentService = new DocumentService(
            _documentRepositoryMock.Object,
            _meetingRepositoryMock.Object,
            _loggerMock.Object,
            settings
        );
    }

    [Fact]
    public async Task UploadDocumentAsync_WithValidFile_UploadsSuccessfully()
    {
        // Arrange
        var meetingId = 1;
        var userId = 1;
        var meeting = new Meeting { Id = meetingId, Title = "Test Meeting" };
        
        var fileMock = new Mock<IFormFile>();
        var content = "Test file content";
        var fileName = "test.pdf";
        var ms = new MemoryStream();
        var writer = new StreamWriter(ms);
        writer.Write(content);
        writer.Flush();
        ms.Position = 0;

        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(ms.Length);
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns((Stream stream, CancellationToken token) => ms.CopyToAsync(stream, token));

        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(meetingId))
            .ReturnsAsync(meeting);
        
        _documentRepositoryMock.Setup(r => r.AddAsync(It.IsAny<MeetingDocument>()))
            .ReturnsAsync((MeetingDocument doc) => doc);

        // Act
        var result = await _documentService.UploadDocumentAsync(meetingId, fileMock.Object, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(fileName, result.FileName);
        Assert.Equal(meetingId, result.MeetingId);
        Assert.Equal(userId, result.UploadedById);
        _documentRepositoryMock.Verify(r => r.AddAsync(It.IsAny<MeetingDocument>()), Times.Once);
    }

    [Fact]
    public async Task UploadDocumentAsync_WithNonExistentMeeting_ThrowsMeetingNotFoundException()
    {
        // Arrange
        var meetingId = 999;
        var userId = 1;
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.pdf");
        fileMock.Setup(f => f.Length).Returns(1024);

        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(meetingId))
            .ReturnsAsync((Meeting?)null);

        // Act & Assert
        await Assert.ThrowsAsync<MeetingNotFoundException>(
            () => _documentService.UploadDocumentAsync(meetingId, fileMock.Object, userId));
    }

    [Fact]
    public async Task ValidateFileAsync_WithValidFile_ReturnsTrue()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.pdf");
        fileMock.Setup(f => f.Length).Returns(1024 * 1024); // 1MB

        // Act
        var result = await _documentService.ValidateFileAsync(fileMock.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateFileAsync_WithOversizedFile_ReturnsFalse()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.pdf");
        fileMock.Setup(f => f.Length).Returns(11 * 1024 * 1024); // 11MB (exceeds 10MB limit)

        // Act
        var result = await _documentService.ValidateFileAsync(fileMock.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateFileAsync_WithInvalidFileType_ReturnsFalse()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.exe");
        fileMock.Setup(f => f.Length).Returns(1024);

        // Act
        var result = await _documentService.ValidateFileAsync(fileMock.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetDocumentByIdAsync_WithExistingDocument_ReturnsDocument()
    {
        // Arrange
        var documentId = 1;
        var document = new MeetingDocument
        {
            Id = documentId,
            FileName = "test.pdf",
            MeetingId = 1
        };

        _documentRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(documentId))
            .ReturnsAsync(document);

        // Act
        var result = await _documentService.GetDocumentByIdAsync(documentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(documentId, result.Id);
        Assert.Equal("test.pdf", result.FileName);
    }

    [Fact]
    public async Task GetMeetingDocumentsAsync_ReturnsDocumentsList()
    {
        // Arrange
        var meetingId = 1;
        var documents = new List<MeetingDocument>
        {
            new MeetingDocument { Id = 1, FileName = "doc1.pdf", MeetingId = meetingId },
            new MeetingDocument { Id = 2, FileName = "doc2.docx", MeetingId = meetingId }
        };

        _documentRepositoryMock.Setup(r => r.GetByMeetingIdAsync(meetingId))
            .ReturnsAsync(documents);

        // Act
        var result = await _documentService.GetMeetingDocumentsAsync(meetingId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task DeleteDocumentAsync_WithExistingDocument_DeletesSuccessfully()
    {
        // Arrange
        var documentId = 1;
        var userId = 1;
        var fileName = Guid.NewGuid() + ".pdf";
        var filePath = Path.Combine(_testUploadPath, fileName);
        
        // Create a test file
        await File.WriteAllTextAsync(filePath, "test content");

        var document = new MeetingDocument
        {
            Id = documentId,
            FileName = "test.pdf",
            FilePath = fileName,
            MeetingId = 1
        };

        _documentRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(documentId))
            .ReturnsAsync(document);
        
        _documentRepositoryMock.Setup(r => r.DeleteAsync(document))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _documentService.DeleteDocumentAsync(documentId, userId);

        // Assert
        Assert.True(result);
        Assert.False(File.Exists(filePath));
        _documentRepositoryMock.Verify(r => r.DeleteAsync(document), Times.Once);
    }

    [Fact]
    public async Task DeleteDocumentAsync_WithNonExistentDocument_ThrowsDocumentNotFoundException()
    {
        // Arrange
        var documentId = 999;
        var userId = 1;

        _documentRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(documentId))
            .ReturnsAsync((MeetingDocument?)null);

        // Act & Assert
        await Assert.ThrowsAsync<DocumentNotFoundException>(
            () => _documentService.DeleteDocumentAsync(documentId, userId));
    }

    public void Dispose()
    {
        // Clean up test upload directory
        if (Directory.Exists(_testUploadPath))
        {
            Directory.Delete(_testUploadPath, true);
        }
    }
}
