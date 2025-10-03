using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using MedicalAI.Core.Security;
using MedicalAI.Core.Performance;

namespace MedicalAI.Infrastructure.Security
{
    public class EnhancedDicomAnonymizer : IEnhancedDicomAnonymizer
    {
        private readonly ILogger<EnhancedDicomAnonymizer> _logger;
        private readonly IParallelProcessor _parallelProcessor;
        private readonly IAuditLogger _auditLogger;
        private readonly ISecurityService _securityService;

        // Comprehensive list of DICOM tags that may contain patient identifiers
        private static readonly DicomTag[] IdentifyingTags = new[]
        {
            DicomTag.PatientName,
            DicomTag.PatientID,
            DicomTag.PatientBirthDate,
            DicomTag.PatientSex,
            DicomTag.PatientAge,
            DicomTag.PatientWeight,
            DicomTag.PatientAddress,
            DicomTag.PatientTelephoneNumbers,
            DicomTag.InstitutionName,
            DicomTag.InstitutionAddress,
            DicomTag.ReferringPhysicianName,
            DicomTag.PerformingPhysicianName,
            DicomTag.OperatorsName,
            DicomTag.StudyDate,
            DicomTag.SeriesDate,
            DicomTag.AcquisitionDate,
            DicomTag.ContentDate,
            DicomTag.StudyTime,
            DicomTag.SeriesTime,
            DicomTag.AcquisitionTime,
            DicomTag.ContentTime,
            DicomTag.AccessionNumber,
            DicomTag.StudyID,
            DicomTag.RequestedProcedureDescription,
            DicomTag.PerformedProcedureStepDescription,
            DicomTag.StudyDescription,
            DicomTag.SeriesDescription,
            DicomTag.ProtocolName,
            DicomTag.DeviceSerialNumber,
            DicomTag.SoftwareVersions,
            DicomTag.StationName
        };

        public EnhancedDicomAnonymizer(
            ILogger<EnhancedDicomAnonymizer> logger,
            IParallelProcessor parallelProcessor,
            IAuditLogger auditLogger,
            ISecurityService securityService)
        {
            _logger = logger;
            _parallelProcessor = parallelProcessor;
            _auditLogger = auditLogger;
            _securityService = securityService;
        }

        public async Task<AnonymizationResult> AnonymizeAsync(
            IEnumerable<string> filePaths,
            AnonymizationProfile profile,
            CancellationToken cancellationToken = default)
        {
            var filePathsList = filePaths.ToList();
            var batchId = Guid.NewGuid().ToString();
            var errors = new List<string>();

            _logger.LogInformation("Starting enhanced DICOM anonymization. Batch: {BatchId}, Files: {FileCount}, Profile: {Profile}", 
                batchId, filePathsList.Count, profile.Name);

            // Validate files first
            var validationTasks = filePathsList.Select(async filePath =>
            {
                var validation = await _securityService.ValidateFileAsync(filePath, cancellationToken);
                return new { FilePath = filePath, Validation = validation };
            });

            var validationResults = await Task.WhenAll(validationTasks);
            var validFiles = validationResults
                .Where(r => r.Validation.IsValid && r.Validation.IsSafe)
                .Select(r => r.FilePath)
                .ToList();

            // Log validation failures
            foreach (var entry in validationResults.Where(r => !r.Validation.IsValid || !r.Validation.IsSafe))
            {
                var error = $"File validation failed for {entry.FilePath}: {string.Join(", ", entry.Validation.SecurityIssues)}";
                errors.Add(error);
                _logger.LogWarning(error);
            }

            // Process valid files in parallel
            var results = await _parallelProcessor.ProcessInParallelAsync(
                validFiles,
                async (filePath, ct) => await AnonymizeFileAsync(filePath, profile, batchId, ct),
                maxConcurrency: Math.Max(1, Environment.ProcessorCount / 2),
                cancellationToken);

            var successfulFiles = results.Count(success => success);
            var failedFiles = results.Count(success => !success);

            if (failedFiles > 0)
            {
                errors.Add($"{failedFiles} files failed to anonymize");
            }

            var result = new AnonymizationResult(
                filePathsList.Count,
                successfulFiles,
                errors,
                batchId);

            _logger.LogInformation("Anonymization completed. Batch: {BatchId}, Successful: {Successful}/{Total}", 
                batchId, successfulFiles, filePathsList.Count);

            return result;
        }

        public async Task<ValidationResult> ValidateAnonymizationAsync(
            string filePath,
            AnonymizationProfile profile,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Validating anonymization for file: {FilePath}", filePath);

            try
            {
                var dcm = await DicomFile.OpenAsync(filePath);
                var remainingIdentifiers = new List<string>();
                var totalTags = 0;
                var anonymizedTags = 0;

                // Check all identifying tags
                foreach (var tag in IdentifyingTags)
                {
                    totalTags++;
                    if (dcm.Dataset.Contains(tag))
                    {
                        var value = dcm.Dataset.GetSingleValueOrDefault(tag, string.Empty);
                        if (!string.IsNullOrEmpty(value) && !IsAnonymizedValue(value))
                        {
                            remainingIdentifiers.Add($"{tag}: {value}");
                        }
                        else
                        {
                            anonymizedTags++;
                        }
                    }
                    else
                    {
                        anonymizedTags++; // Tag removed completely
                    }
                }

                // Check private tags if profile requires their removal
                if (profile.RemovePrivateTags)
                {
                    var privateTags = dcm.Dataset.Where(item => item.Tag.IsPrivate).ToList();
                    foreach (var privateTag in privateTags)
                    {
                        totalTags++;
                        remainingIdentifiers.Add($"Private tag: {privateTag.Tag}");
                    }
                }

                var privacyScore = totalTags > 0 ? (double)anonymizedTags / totalTags : 1.0;
                var isFullyAnonymized = remainingIdentifiers.Count == 0;

                _logger.LogDebug("Anonymization validation completed. Fully anonymized: {IsFullyAnonymized}, Privacy score: {PrivacyScore:F2}", 
                    isFullyAnonymized, privacyScore);

                return new ValidationResult(isFullyAnonymized, remainingIdentifiers, privacyScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating anonymization for file: {FilePath}", filePath);
                return new ValidationResult(false, new[] { $"Validation error: {ex.Message}" }, 0.0);
            }
        }

        public async Task<AuditTrail> CreateAuditTrailAsync(
            AnonymizationResult result,
            string userId,
            CancellationToken cancellationToken = default)
        {
            var auditTrail = new AuditTrail(
                result.BatchId,
                DateTime.UtcNow,
                userId,
                new AnonymizationProfile("Enhanced", Enumerable.Empty<string>(), Enumerable.Empty<string>()),
                result);

            // Log the audit event
            await _auditLogger.LogSecurityEventAsync(
                new SecurityEvent(
                    "DICOM_ANONYMIZATION",
                    $"Anonymized {result.FilesSuccessful}/{result.FilesProcessed} files in batch {result.BatchId}",
                    DateTime.UtcNow,
                    userId,
                    null,
                    SecurityEventSeverity.Medium),
                cancellationToken);

            _logger.LogInformation("Audit trail created for batch: {BatchId}", result.BatchId);
            return auditTrail;
        }

        private async Task<bool> AnonymizeFileAsync(
            string filePath,
            AnonymizationProfile profile,
            string batchId,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Anonymizing file: {FilePath}", filePath);

                var dcm = await DicomFile.OpenAsync(filePath);
                var dataset = dcm.Dataset;

                // Create backup if needed
                var backupPath = $"{filePath}.backup.{batchId}";
                await dcm.SaveAsync(backupPath);

                // Remove or replace identifying tags
                foreach (var tag in IdentifyingTags)
                {
                    if (dataset.Contains(tag))
                    {
                        if (profile.TagsToRemove.Contains(tag.ToString()) || 
                            profile.TagsToRemove.Contains("ALL"))
                        {
                            dataset.Remove(tag);
                        }
                        else if (profile.TagsToReplace.Contains(tag.ToString()))
                        {
                            dataset.AddOrUpdate(tag, GetAnonymizedValue(tag));
                        }
                        else
                        {
                            // Default anonymization
                            dataset.AddOrUpdate(tag, GetAnonymizedValue(tag));
                        }
                    }
                }

                // Remove private tags if requested
                if (profile.RemovePrivateTags)
                {
                    var privateTags = dataset.Where(item => item.Tag.IsPrivate).ToList();
                    foreach (var privateTag in privateTags)
                    {
                        dataset.Remove(privateTag.Tag);
                    }
                }

                // Remove pixel data if requested (for maximum privacy)
                if (profile.RemovePixelData && dataset.Contains(DicomTag.PixelData))
                {
                    dataset.Remove(DicomTag.PixelData);
                }

                // Add anonymization metadata
                dataset.AddOrUpdate(DicomTag.DeidentificationMethod, "Enhanced Anonymization");
                dataset.AddOrUpdate(DicomTag.DeidentificationMethodCodeSequence, batchId);
                dataset.AddOrUpdate(new DicomTag(0x0012, 0x0063), DateTime.UtcNow.ToString("yyyyMMddHHmmss")); // De-identification DateTime

                // Save anonymized file
                await dcm.SaveAsync(filePath);

                _logger.LogDebug("File anonymized successfully: {FilePath}", filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to anonymize file: {FilePath}", filePath);
                return false;
            }
        }

        private static string GetAnonymizedValue(DicomTag tag)
        {
            return tag.DictionaryEntry.ValueRepresentations.First().Code switch
            {
                "PN" => "ANONYMOUS^PATIENT", // Person Name
                "DA" => "19000101", // Date
                "TM" => "000000", // Time
                "DT" => "19000101000000", // DateTime
                "LO" or "SH" or "CS" => "ANONYMIZED", // Text fields
                "UI" => DicomUIDGenerator.GenerateDerivedFromUUID().UID, // Unique Identifier
                "IS" or "DS" => "0", // Numbers
                _ => "ANONYMIZED"
            };
        }

        private static bool IsAnonymizedValue(string value)
        {
            var anonymizedPatterns = new[]
            {
                "ANONYMOUS",
                "ANONYMIZED",
                "REMOVED",
                "19000101",
                "000000"
            };

            return anonymizedPatterns.Any(pattern => 
                value.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }
    }
}
