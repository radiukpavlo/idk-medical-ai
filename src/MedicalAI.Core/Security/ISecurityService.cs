using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MedicalAI.Core.Security
{
    /// <summary>
    /// Interface for medical data security and privacy services
    /// </summary>
    public interface ISecurityService
    {
        /// <summary>
        /// Validates file integrity and security
        /// </summary>
        Task<FileValidationResult> ValidateFileAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Encrypts sensitive medical data
        /// </summary>
        Task<byte[]> EncryptDataAsync(byte[] data, string keyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Decrypts sensitive medical data
        /// </summary>
        Task<byte[]> DecryptDataAsync(byte[] encryptedData, string keyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates secure hash for data integrity verification
        /// </summary>
        string GenerateSecureHash(byte[] data);

        /// <summary>
        /// Verifies data integrity using hash
        /// </summary>
        bool VerifyDataIntegrity(byte[] data, string expectedHash);
    }

    /// <summary>
    /// Interface for DICOM anonymization with enhanced privacy features
    /// </summary>
    public interface IEnhancedDicomAnonymizer
    {
        /// <summary>
        /// Anonymizes DICOM files with comprehensive privacy protection
        /// </summary>
        Task<AnonymizationResult> AnonymizeAsync(
            IEnumerable<string> filePaths,
            AnonymizationProfile profile,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates anonymization completeness
        /// </summary>
        Task<ValidationResult> ValidateAnonymizationAsync(
            string filePath,
            AnonymizationProfile profile,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates anonymization audit trail
        /// </summary>
        Task<AuditTrail> CreateAuditTrailAsync(
            AnonymizationResult result,
            string userId,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for audit logging of medical data access
    /// </summary>
    public interface IAuditLogger
    {
        /// <summary>
        /// Logs data access event
        /// </summary>
        Task LogDataAccessAsync(DataAccessEvent accessEvent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs security event
        /// </summary>
        Task LogSecurityEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves audit logs for a specific time period
        /// </summary>
        Task<IEnumerable<AuditLogEntry>> GetAuditLogsAsync(
            DateTime fromDate,
            DateTime toDate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Exports audit logs for compliance reporting
        /// </summary>
        Task<string> ExportAuditLogsAsync(
            DateTime fromDate,
            DateTime toDate,
            string format = "json",
            CancellationToken cancellationToken = default);
    }

    // Data models
    public record FileValidationResult(
        bool IsValid,
        bool IsSafe,
        IEnumerable<string> SecurityIssues,
        string FileHash);

    public record AnonymizationProfile(
        string Name,
        IEnumerable<string> TagsToRemove,
        IEnumerable<string> TagsToReplace,
        bool RemovePrivateTags = true,
        bool RemovePixelData = false);

    public record AnonymizationResult(
        int FilesProcessed,
        int FilesSuccessful,
        IEnumerable<string> Errors,
        string BatchId);

    public record ValidationResult(
        bool IsFullyAnonymized,
        IEnumerable<string> RemainingIdentifiers,
        double PrivacyScore);

    public record AuditTrail(
        string BatchId,
        DateTime Timestamp,
        string UserId,
        AnonymizationProfile Profile,
        AnonymizationResult Result);

    public record DataAccessEvent(
        string UserId,
        string ResourcePath,
        string Action,
        DateTime Timestamp,
        string? AdditionalInfo = null);

    public record SecurityEvent(
        string EventType,
        string Description,
        DateTime Timestamp,
        string? UserId = null,
        string? ResourcePath = null,
        SecurityEventSeverity Severity = SecurityEventSeverity.Medium);

    public record AuditLogEntry(
        Guid Id,
        DateTime Timestamp,
        string EventType,
        string UserId,
        string Description,
        string? ResourcePath = null,
        string? AdditionalData = null);

    public enum SecurityEventSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
}