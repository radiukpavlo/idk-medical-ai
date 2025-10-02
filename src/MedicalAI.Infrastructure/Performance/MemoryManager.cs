using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MedicalAI.Core.Performance;

namespace MedicalAI.Infrastructure.Performance
{
    public class MemoryManager : IMemoryManager, IDisposable
    {
        private readonly ILogger<MemoryManager> _logger;
        private readonly Timer? _memoryMonitorTimer;
        private readonly long _memoryPressureThreshold;
        private bool _disposed;

        public MemoryManager(ILogger<MemoryManager> logger)
        {
            _logger = logger;
            // Set memory pressure threshold to 80% of available memory
            _memoryPressureThreshold = (long)(GC.GetTotalMemory(false) * 0.8);
        }

        public long GetCurrentMemoryUsage()
        {
            return GC.GetTotalMemory(false);
        }

        public long GetAvailableMemory()
        {
            var process = Process.GetCurrentProcess();
            return process.WorkingSet64;
        }

        public bool HasSufficientMemory(long requiredBytes)
        {
            var currentUsage = GetCurrentMemoryUsage();
            var availableMemory = GetAvailableMemory();
            
            // Check if we have enough memory with a safety margin
            var safetyMargin = requiredBytes * 0.2; // 20% safety margin
            return (currentUsage + requiredBytes + safetyMargin) < availableMemory;
        }

        public async Task ForceCleanupAsync()
        {
            _logger.LogInformation("Starting memory cleanup");
            
            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            // Give the GC time to complete
            await Task.Delay(100);
            
            _logger.LogInformation("Memory cleanup completed. Current usage: {MemoryUsage} bytes", 
                GetCurrentMemoryUsage());
        }

        public void StartMemoryPressureMonitoring(CancellationToken cancellationToken)
        {
            if (_disposed)
                return;

            var timer = new Timer(async _ =>
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                var currentUsage = GetCurrentMemoryUsage();
                if (currentUsage > _memoryPressureThreshold)
                {
                    _logger.LogWarning("Memory pressure detected. Current usage: {MemoryUsage} bytes, Threshold: {Threshold} bytes", 
                        currentUsage, _memoryPressureThreshold);
                    
                    await ForceCleanupAsync();
                }
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

            cancellationToken.Register(() => timer?.Dispose());
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _memoryMonitorTimer?.Dispose();
                _disposed = true;
            }
        }
    }

    public class LargeImageManager : ILargeImageManager
    {
        private readonly ILogger<LargeImageManager> _logger;
        private readonly IMemoryManager _memoryManager;

        public LargeImageManager(ILogger<LargeImageManager> logger, IMemoryManager memoryManager)
        {
            _logger = logger;
            _memoryManager = memoryManager;
        }

        public async Task<T> LoadWithStreamingAsync<T>(string filePath, Func<byte[], T> processor, CancellationToken cancellationToken)
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
                throw new FileNotFoundException($"File not found: {filePath}");

            _logger.LogInformation("Loading large image file: {FilePath}, Size: {FileSize} bytes", filePath, fileInfo.Length);

            // Check if we have enough memory for the file
            if (!_memoryManager.HasSufficientMemory(fileInfo.Length))
            {
                _logger.LogWarning("Insufficient memory for file {FilePath}. Attempting cleanup.", filePath);
                await _memoryManager.ForceCleanupAsync();
                
                if (!_memoryManager.HasSufficientMemory(fileInfo.Length))
                {
                    throw new OutOfMemoryException($"Insufficient memory to load file {filePath}");
                }
            }

            // Use memory-mapped file for very large files (>100MB)
            if (fileInfo.Length > 100 * 1024 * 1024)
            {
                using var mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, "image", fileInfo.Length);
                using var accessor = mmf.CreateViewAccessor(0, fileInfo.Length);
                
                var data = new byte[fileInfo.Length];
                accessor.ReadArray(0, data, 0, (int)fileInfo.Length);
                
                return processor(data);
            }
            else
            {
                // For smaller files, use regular file reading
                var data = await File.ReadAllBytesAsync(filePath, cancellationToken);
                return processor(data);
            }
        }

        public async Task ProcessInChunksAsync(byte[] imageData, int chunkSize, Func<byte[], int, Task> chunkProcessor, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing image data in chunks. Total size: {TotalSize} bytes, Chunk size: {ChunkSize} bytes", 
                imageData.Length, chunkSize);

            for (int offset = 0; offset < imageData.Length; offset += chunkSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var currentChunkSize = Math.Min(chunkSize, imageData.Length - offset);
                var chunk = new byte[currentChunkSize];
                Array.Copy(imageData, offset, chunk, 0, currentChunkSize);

                await chunkProcessor(chunk, offset);

                // Check memory pressure after each chunk
                if (_memoryManager.GetCurrentMemoryUsage() > _memoryManager.GetAvailableMemory() * 0.7)
                {
                    await _memoryManager.ForceCleanupAsync();
                }
            }
        }

        public IDisposable CreateMemoryMappedView(string filePath, out byte[] data)
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
                throw new FileNotFoundException($"File not found: {filePath}");

            var mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, "image", fileInfo.Length);
            var accessor = mmf.CreateViewAccessor(0, fileInfo.Length);
            
            data = new byte[fileInfo.Length];
            accessor.ReadArray(0, data, 0, (int)fileInfo.Length);

            return new MemoryMappedViewDisposable(mmf, accessor);
        }

        private class MemoryMappedViewDisposable : IDisposable
        {
            private readonly MemoryMappedFile _mmf;
            private readonly MemoryMappedViewAccessor _accessor;

            public MemoryMappedViewDisposable(MemoryMappedFile mmf, MemoryMappedViewAccessor accessor)
            {
                _mmf = mmf;
                _accessor = accessor;
            }

            public void Dispose()
            {
                _accessor?.Dispose();
                _mmf?.Dispose();
            }
        }
    }
}