using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MedicalAI.Core.Performance
{
    /// <summary>
    /// Interface for parallel processing of AI model inference
    /// </summary>
    public interface IParallelProcessor
    {
        /// <summary>
        /// Processes multiple items in parallel with controlled concurrency
        /// </summary>
        Task<IEnumerable<TResult>> ProcessInParallelAsync<TInput, TResult>(
            IEnumerable<TInput> items,
            Func<TInput, CancellationToken, Task<TResult>> processor,
            int maxConcurrency,
            CancellationToken cancellationToken);

        /// <summary>
        /// Processes a batch of AI inference tasks in parallel
        /// </summary>
        Task<IEnumerable<TResult>> ProcessBatchAsync<TInput, TResult>(
            IEnumerable<TInput> batch,
            Func<TInput, CancellationToken, Task<TResult>> processor,
            CancellationToken cancellationToken);

        /// <summary>
        /// Partitions large datasets for parallel processing
        /// </summary>
        IEnumerable<IEnumerable<T>> PartitionForParallelProcessing<T>(IEnumerable<T> source, int partitionSize);
    }

    /// <summary>
    /// Interface for background task management to keep UI responsive
    /// </summary>
    public interface IBackgroundTaskManager
    {
        /// <summary>
        /// Executes a long-running task in the background with progress reporting
        /// </summary>
        Task<T> ExecuteBackgroundTaskAsync<T>(
            Func<IProgress<ProgressInfo>, CancellationToken, Task<T>> task,
            CancellationToken cancellationToken);

        /// <summary>
        /// Queues a task for background execution
        /// </summary>
        Task QueueBackgroundTaskAsync(
            Func<CancellationToken, Task> task,
            TaskPriority priority = TaskPriority.Normal,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current number of queued background tasks
        /// </summary>
        int GetQueuedTaskCount();
    }

    public record ProgressInfo(int Current, int Total, string? Message = null);

    public enum TaskPriority
    {
        Low,
        Normal,
        High,
        Critical
    }
}