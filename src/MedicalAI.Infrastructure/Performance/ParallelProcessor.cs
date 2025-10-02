using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MedicalAI.Core.Performance;

namespace MedicalAI.Infrastructure.Performance
{
    public class ParallelProcessor : IParallelProcessor
    {
        private readonly ILogger<ParallelProcessor> _logger;
        private readonly int _defaultMaxConcurrency;

        public ParallelProcessor(ILogger<ParallelProcessor> logger)
        {
            _logger = logger;
            _defaultMaxConcurrency = Math.Max(1, Environment.ProcessorCount - 1); // Leave one core for UI
        }

        public async Task<IEnumerable<TResult>> ProcessInParallelAsync<TInput, TResult>(
            IEnumerable<TInput> items,
            Func<TInput, CancellationToken, Task<TResult>> processor,
            int maxConcurrency,
            CancellationToken cancellationToken)
        {
            var itemsList = items.ToList();
            _logger.LogInformation("Processing {ItemCount} items in parallel with max concurrency: {MaxConcurrency}", 
                itemsList.Count, maxConcurrency);

            var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            var results = new ConcurrentBag<TResult>();

            var tasks = itemsList.Select(async item =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var result = await processor(item, cancellationToken);
                    results.Add(result);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            return results;
        }

        public async Task<IEnumerable<TResult>> ProcessBatchAsync<TInput, TResult>(
            IEnumerable<TInput> batch,
            Func<TInput, CancellationToken, Task<TResult>> processor,
            CancellationToken cancellationToken)
        {
            return await ProcessInParallelAsync(batch, processor, _defaultMaxConcurrency, cancellationToken);
        }

        public IEnumerable<IEnumerable<T>> PartitionForParallelProcessing<T>(IEnumerable<T> source, int partitionSize)
        {
            var sourceList = source.ToList();
            _logger.LogInformation("Partitioning {ItemCount} items into partitions of size {PartitionSize}", 
                sourceList.Count, partitionSize);

            for (int i = 0; i < sourceList.Count; i += partitionSize)
            {
                yield return sourceList.Skip(i).Take(partitionSize);
            }
        }
    }

    public class BackgroundTaskManager : IBackgroundTaskManager, IDisposable
    {
        private readonly ILogger<BackgroundTaskManager> _logger;
        private readonly Channel<BackgroundTask> _taskQueue;
        private readonly ChannelWriter<BackgroundTask> _writer;
        private readonly ChannelReader<BackgroundTask> _reader;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _processingTask;
        private bool _disposed;

        public BackgroundTaskManager(ILogger<BackgroundTaskManager> logger)
        {
            _logger = logger;
            var options = new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            };

            var channel = Channel.CreateBounded<BackgroundTask>(options);
            _taskQueue = channel;
            _writer = channel.Writer;
            _reader = channel.Reader;
            _cancellationTokenSource = new CancellationTokenSource();
            
            _processingTask = ProcessTasksAsync(_cancellationTokenSource.Token);
        }

        public async Task<T> ExecuteBackgroundTaskAsync<T>(
            Func<IProgress<ProgressInfo>, CancellationToken, Task<T>> task,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Executing background task with progress reporting");

            var tcs = new TaskCompletionSource<T>();
            var progress = new Progress<ProgressInfo>(info =>
            {
                _logger.LogDebug("Task progress: {Current}/{Total} - {Message}", 
                    info.Current, info.Total, info.Message);
            });

            // Execute on background thread to keep UI responsive
            _ = Task.Run(async () =>
            {
                try
                {
                    var result = await task(progress, cancellationToken);
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background task failed");
                    tcs.SetException(ex);
                }
            }, cancellationToken);

            return await tcs.Task;
        }

        public async Task QueueBackgroundTaskAsync(
            Func<CancellationToken, Task> task,
            TaskPriority priority = TaskPriority.Normal,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BackgroundTaskManager));

            var backgroundTask = new BackgroundTask(task, priority, cancellationToken);
            await _writer.WriteAsync(backgroundTask, cancellationToken);
            
            _logger.LogInformation("Queued background task with priority: {Priority}", priority);
        }

        public int GetQueuedTaskCount()
        {
            // This is an approximation since Channel doesn't expose exact count
            return _reader.CanRead ? 1 : 0;
        }

        private async Task ProcessTasksAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Background task processor started");

            var priorityQueue = new PriorityQueue<BackgroundTask, int>();

            try
            {
                await foreach (var task in _reader.ReadAllAsync(cancellationToken))
                {
                    // Add to priority queue (lower number = higher priority)
                    var priorityValue = task.Priority switch
                    {
                        TaskPriority.Critical => 0,
                        TaskPriority.High => 1,
                        TaskPriority.Normal => 2,
                        TaskPriority.Low => 3,
                        _ => 2
                    };

                    priorityQueue.Enqueue(task, priorityValue);

                    // Process tasks in priority order
                    while (priorityQueue.Count > 0)
                    {
                        var currentTask = priorityQueue.Dequeue();
                        
                        try
                        {
                            await currentTask.TaskFunc(currentTask.CancellationToken);
                            _logger.LogDebug("Background task completed successfully");
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogInformation("Background task was cancelled");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Background task failed");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Background task processor stopped");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _writer.Complete();
                _cancellationTokenSource.Cancel();
                _processingTask.Wait(TimeSpan.FromSeconds(5));
                _cancellationTokenSource.Dispose();
                _disposed = true;
            }
        }

        private record BackgroundTask(
            Func<CancellationToken, Task> TaskFunc,
            TaskPriority Priority,
            CancellationToken CancellationToken);
    }
}