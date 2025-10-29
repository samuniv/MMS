using MeetingManagementSystem.Core.DTOs;
using MeetingManagementSystem.Core.Entities;
using MeetingManagementSystem.Core.Exceptions;
using MeetingManagementSystem.Core.Interfaces;
using MeetingManagementSystem.Core.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MeetingManagementSystem.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IMeetingRepository _meetingRepository;
    private readonly ILogger<DocumentService> _logger;
    private readonly DocumentSettings _settings;
    private readonly string _uploadPath;

    public DocumentService(
        IDocumentRepository documentRepository,
        IMeetingRepository meetingRepository,
        ILogger<DocumentService> logger,
        IOptions<DocumentSettings> settings)
    {
        _documentRepository = documentRepository;
        _meetingRepository = meetingRepository;
        _logger = logger;
        _settings = settings.Value;
        _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), _settings.UploadPath);
        
        // Ensure upload directory exists
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public async Task<MeetingDocument> UploadDocumentAsync(int meetingId, IFormFile file, int uploadedById)
    {
        // Validate meeting exists
        var meeting = await _meetingRepository.GetByIdAsync(meetingId);
        if (meeting == null)
        {
            throw new MeetingNotFoundException(meetingId);
        }

        // Validate file with basic checks
        if (!await ValidateFileAsync(file))
        {
            throw new InvalidFileException("File validation failed. Check file size and type.");
        }

        // Enhanced security validation
        using (var fileStream = file.OpenReadStream())
        {
            var securityValidation = await FileSecurityScanner.ValidateFileAsync(fileStream, file.FileName, file.Length);
            if (!securityValidation.IsValid)
            {
                _logger.LogWarning("File security validation failed: {ErrorMessage}", securityValidation.ErrorMessage);
                throw new InvalidFileException(securityValidation.ErrorMessage);
            }
        }

        // Generate safe unique filename
        var uniqueFileName = FileSecurityScanner.GetSafeFileName(file.FileName);
        var filePath = Path.Combine(_uploadPath, uniqueFileName);

        try
        {
            // Save file to disk
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Create document record
            var document = new MeetingDocument
            {
                MeetingId = meetingId,
                FileName = file.FileName,
                FilePath = uniqueFileName, // Store relative path
                FileSize = file.Length,
                ContentType = file.ContentType,
                UploadedAt = DateTime.UtcNow,
                UploadedById = uploadedById
            };

            await _documentRepository.AddAsync(document);
            
            _logger.LogInformation("Document {FileName} uploaded for meeting {MeetingId} by user {UserId}", 
                file.FileName, meetingId, uploadedById);

            return document;
        }
        catch (Exception ex)
        {
            // Clean up file if database operation fails
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            
            _logger.LogError(ex, "Error uploading document {FileName} for meeting {MeetingId}", 
                file.FileName, meetingId);
            throw;
        }
    }

    public async Task<MeetingDocument?> GetDocumentByIdAsync(int documentId)
    {
        return await _documentRepository.GetByIdWithDetailsAsync(documentId);
    }

    public async Task<IEnumerable<MeetingDocument>> GetMeetingDocumentsAsync(int meetingId)
    {
        return await _documentRepository.GetByMeetingIdAsync(meetingId);
    }

    public async Task<bool> DeleteDocumentAsync(int documentId, int userId)
    {
        var document = await _documentRepository.GetByIdWithDetailsAsync(documentId);
        if (document == null)
        {
            throw new DocumentNotFoundException(documentId);
        }

        try
        {
            // Delete physical file
            var filePath = Path.Combine(_uploadPath, document.FilePath);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            // Delete database record
            await _documentRepository.DeleteAsync(document);
            
            _logger.LogInformation("Document {DocumentId} deleted by user {UserId}", documentId, userId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<(Stream FileStream, string ContentType, string FileName)> GetDocumentStreamAsync(int documentId)
    {
        var document = await _documentRepository.GetByIdWithDetailsAsync(documentId);
        if (document == null)
        {
            throw new DocumentNotFoundException(documentId);
        }

        var filePath = Path.Combine(_uploadPath, document.FilePath);
        if (!File.Exists(filePath))
        {
            _logger.LogError("Physical file not found for document {DocumentId} at path {FilePath}", 
                documentId, filePath);
            throw new FileNotFoundException($"Document file not found: {document.FileName}");
        }

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return (stream, document.ContentType, document.FileName);
    }

    public Task<bool> ValidateFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("File validation failed: File is null or empty");
            return Task.FromResult(false);
        }

        // Check file size (convert MB to bytes)
        var maxSizeBytes = _settings.MaxDocumentSizeMB * 1024 * 1024;
        if (file.Length > maxSizeBytes)
        {
            _logger.LogWarning("File validation failed: File size {FileSize} exceeds maximum {MaxSize}", 
                file.Length, maxSizeBytes);
            return Task.FromResult(false);
        }

        // Check file extension
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_settings.AllowedFileTypes.Contains(fileExtension))
        {
            _logger.LogWarning("File validation failed: File type {FileType} not allowed", fileExtension);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}
