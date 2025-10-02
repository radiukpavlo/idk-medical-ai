using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.IO;
using Microsoft.Extensions.Logging;
using MedicalAI.Core;
using MedicalAI.Core.Imaging;
using MedicalAI.Core.Performance;

namespace MedicalAI.Infrastructure.Imaging
{
    public class DicomImportService : IDicomImportService
    {
        private readonly ILogger<DicomImportService> _logger;
        private readonly IParallelProcessor _parallelProcessor;
        private readonly IMemoryManager _memoryManager;

        public DicomImportService(
            ILogger<DicomImportService> logger,
            IParallelProcessor parallelProcessor,
            IMemoryManager memoryManager)
        {
            _logger = logger;
            _parallelProcessor = parallelProcessor;
            _memoryManager = memoryManager;
        }

        public async Task<ImportResult> ImportAsync(string path, DicomImportOptions options, CancellationToken ct)
        {
            _logger.LogInformation("Starting DICOM import from path: {Path}", path);
            
            var files = Directory.EnumerateFiles(path, "*.dcm", SearchOption.AllDirectories).ToList();
            _logger.LogInformation("Found {FileCount} DICOM files to process", files.Count);

            // Process files in parallel with memory management
            var results = await _parallelProcessor.ProcessInParallelAsync(
                files,
                async (file, cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    // Check memory before processing each file
                    var fileInfo = new FileInfo(file);
                    if (!_memoryManager.HasSufficientMemory(fileInfo.Length))
                    {
                        await _memoryManager.ForceCleanupAsync();
                    }

                    try
                    {
                        var dcm = await DicomFile.OpenAsync(file);
                        var studyUid = dcm.Dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, Guid.NewGuid().ToString());
                        var seriesUid = dcm.Dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, Guid.NewGuid().ToString());
                        return new { StudyUid = studyUid, SeriesUid = seriesUid, Success = true };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process DICOM file: {File}", file);
                        return new { StudyUid = "", SeriesUid = "", Success = false };
                    }
                },
                maxConcurrency: Math.Max(1, Environment.ProcessorCount / 2), // Use half the cores
                ct);

            var successfulResults = results.Where(r => r.Success).ToList();
            var images = successfulResults.Count;
            var uniqueSeries = successfulResults.Select(r => r.SeriesUid).Distinct().Count();
            var uniqueStudies = successfulResults.Select(r => r.StudyUid).Distinct().Count();

            _logger.LogInformation("DICOM import completed. Studies: {Studies}, Series: {Series}, Images: {Images}", 
                uniqueStudies, uniqueSeries, images);

            return new ImportResult(uniqueStudies, uniqueSeries, images);
        }
    }

    public class DicomAnonymizerService : IDicomAnonymizerService
    {
        private readonly ILogger<DicomAnonymizerService> _logger;
        private readonly IParallelProcessor _parallelProcessor;
        private readonly IMemoryManager _memoryManager;

        public DicomAnonymizerService(
            ILogger<DicomAnonymizerService> logger,
            IParallelProcessor parallelProcessor,
            IMemoryManager memoryManager)
        {
            _logger = logger;
            _parallelProcessor = parallelProcessor;
            _memoryManager = memoryManager;
        }

        public async Task<int> AnonymizeInPlaceAsync(IEnumerable<string> filePaths, AnonymizerProfile profile, CancellationToken ct)
        {
            var filePathsList = filePaths.ToList();
            _logger.LogInformation("Starting anonymization of {FileCount} DICOM files with profile: {Profile}", 
                filePathsList.Count, profile.Name);

            var results = await _parallelProcessor.ProcessInParallelAsync(
                filePathsList,
                async (filePath, cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    // Check memory before processing each file
                    var fileInfo = new FileInfo(filePath);
                    if (!_memoryManager.HasSufficientMemory(fileInfo.Length * 2)) // Need extra memory for processing
                    {
                        await _memoryManager.ForceCleanupAsync();
                    }

                    try
                    {
                        var file = await DicomFile.OpenAsync(filePath);
                        var anon = new DicomAnonymizer();
                        anon.AnonymizeInPlace(file.Dataset);
                        await file.SaveAsync(filePath);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to anonymize DICOM file: {FilePath}", filePath);
                        return false;
                    }
                },
                maxConcurrency: Math.Max(1, Environment.ProcessorCount / 2),
                ct);

            var successCount = results.Count(success => success);
            _logger.LogInformation("Anonymization completed. Successfully processed: {SuccessCount}/{TotalCount} files", 
                successCount, filePathsList.Count);

            return successCount;
        }
    }

    public class VolumeStore : IVolumeStore
    {
        private readonly ILogger<VolumeStore> _logger;
        private readonly ILargeImageManager _largeImageManager;
        private readonly IMemoryManager _memoryManager;

        public VolumeStore(
            ILogger<VolumeStore> logger,
            ILargeImageManager largeImageManager,
            IMemoryManager memoryManager)
        {
            _logger = logger;
            _largeImageManager = largeImageManager;
            _memoryManager = memoryManager;
        }

        public async Task<Volume3D> LoadAsync(ImageRef imageRef, CancellationToken ct)
        {
            _logger.LogInformation("Loading volume from: {FilePath}", imageRef.FilePath);

            if (imageRef.FilePath.EndsWith(".nii", StringComparison.OrdinalIgnoreCase))
            {
                // Use memory-efficient loading for NIfTI files
                return await _largeImageManager.LoadWithStreamingAsync(
                    imageRef.FilePath,
                    data =>
                    {
                        var (w, h, d, vx, vy, vz, volumeData) = NiftiReader.ReadFromBytes(data);
                        return new Volume3D(w, h, d, vx, vy, vz, volumeData);
                    },
                    ct);
            }

            // Optimized DICOM loading with memory management
            var fileInfo = new FileInfo(imageRef.FilePath);
            if (!_memoryManager.HasSufficientMemory(fileInfo.Length * 3)) // Extra memory for processing
            {
                await _memoryManager.ForceCleanupAsync();
            }

            var dcm = await DicomFile.OpenAsync(imageRef.FilePath);
            var pixelData = DicomPixelData.Create(dcm.Dataset);
            var bytes = pixelData.GetFrame(0).Data;
            int rows = dcm.Dataset.GetSingleValueOrDefault(DicomTag.Rows, 0);
            int cols = dcm.Dataset.GetSingleValueOrDefault(DicomTag.Columns, 0);
            
            var volume = new byte[rows * cols];
            bytes.CopyTo(volume, 0);
            
            _logger.LogInformation("Volume loaded successfully. Dimensions: {Cols}x{Rows}x1", cols, rows);
            return new Volume3D(cols, rows, 1, 1, 1, 1, volume);
        }

        public async Task SaveMaskAsync(ImageRef imageRef, Mask3D mask, CancellationToken ct)
        {
            var outPath = Path.ChangeExtension(imageRef.FilePath, ".mask.bin");
            _logger.LogInformation("Saving mask to: {OutPath}", outPath);

            // Use chunked writing for large masks
            const int chunkSize = 1024 * 1024; // 1MB chunks
            if (mask.Labels.Length > chunkSize)
            {
                await _largeImageManager.ProcessInChunksAsync(
                    mask.Labels,
                    chunkSize,
                    async (chunk, offset) =>
                    {
                        using var fs = new FileStream(outPath, offset == 0 ? FileMode.Create : FileMode.Append);
                        await fs.WriteAsync(chunk, 0, chunk.Length, ct);
                    },
                    ct);
            }
            else
            {
                await File.WriteAllBytesAsync(outPath, mask.Labels, ct);
            }

            _logger.LogInformation("Mask saved successfully");
        }
    }
}
