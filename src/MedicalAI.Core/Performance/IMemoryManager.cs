using System;
using System.Threading;
using System.Threading.Tasks;

namespace MedicalAI.Core.Performance
{
    /// <summary>
    /// Interface for managing memory usage in medical image processing
    /// </summary>
    public interface IMemoryManager
    {
        /// <summary>
        /// Gets current memory usage in bytes
        /// </summary>
        long GetCurrentMemoryUsage();

        /// <summary>
        /// Gets available memory in bytes
        /// </summary>
        long GetAvailableMemory();

        /// <summary>
        /// Checks if there's enough memory for an operation
        /// </summary>
        bool HasSufficientMemory(long requiredBytes);

        /// <summary>
        /// Forces garbage collection and memory cleanup
        /// </summary>
        Task ForceCleanupAsync();

        /// <summary>
        /// Monitors memory pressure and triggers cleanup when needed
        /// </summary>
        void StartMemoryPressureMonitoring(CancellationToken cancellationToken);
    }

    /// <summary>
    /// Interface for managing large medical image data with memory optimization
    /// </summary>
    public interface ILargeImageManager
    {
        /// <summary>
        /// Loads image data with memory-efficient streaming
        /// </summary>
        Task<T> LoadWithStreamingAsync<T>(string filePath, Func<byte[], T> processor, CancellationToken cancellationToken);

        /// <summary>
        /// Processes image data in chunks to reduce memory footprint
        /// </summary>
        Task ProcessInChunksAsync(byte[] imageData, int chunkSize, Func<byte[], int, Task> chunkProcessor, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a memory-mapped view of large image files
        /// </summary>
        IDisposable CreateMemoryMappedView(string filePath, out byte[] data);
    }
}