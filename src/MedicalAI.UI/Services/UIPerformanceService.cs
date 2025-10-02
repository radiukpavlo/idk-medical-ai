using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using MedicalAI.Core.Performance;

namespace MedicalAI.UI.Services
{
    /// <summary>
    /// Service to manage UI responsiveness during heavy operations
    /// </summary>
    public interface IUIPerformanceService
    {
        /// <summary>
        /// Executes a long-running operation while keeping the UI responsive
        /// </summary>
        Task<T> ExecuteWithProgressAsync<T>(
            Func<IProgress<ProgressInfo>, CancellationToken, Task<T>> operation,
            Action<ProgressInfo>? progressCallback = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an operation on a background thread and marshals the result back to the UI thread
        /// </summary>
        Task<T> ExecuteOnBackgroundThreadAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Yields control to allow UI updates during intensive operations
        /// </summary>
        Task YieldToUIAsync();
    }

    public class UIPerformanceService : IUIPerformanceService
    {
        private readonly ILogger<UIPerformanceService> _logger;
        private readonly IBackgroundTaskManager _backgroundTaskManager;

        public UIPerformanceService(
            ILogger<UIPerformanceService> logger,
            IBackgroundTaskManager backgroundTaskManager)
        {
            _logger = logger;
            _backgroundTaskManager = backgroundTaskManager;
        }

        public async Task<T> ExecuteWithProgressAsync<T>(
            Func<IProgress<ProgressInfo>, CancellationToken, Task<T>> operation,
            Action<ProgressInfo>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting UI operation with progress reporting");

            var progress = new Progress<ProgressInfo>(info =>
            {
                // Marshal progress updates to the UI thread
                Dispatcher.UIThread.Post(() =>
                {
                    progressCallback?.Invoke(info);
                });
            });

            try
            {
                // Execute the operation using the background task manager
                var result = await _backgroundTaskManager.ExecuteBackgroundTaskAsync(operation, cancellationToken);
                
                _logger.LogInformation("UI operation completed successfully");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UI operation failed");
                throw;
            }
        }

        public async Task<T> ExecuteOnBackgroundThreadAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing operation on background thread");

            var tcs = new TaskCompletionSource<T>();

            await _backgroundTaskManager.QueueBackgroundTaskAsync(async ct =>
            {
                try
                {
                    var result = await operation(ct);
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, TaskPriority.Normal, cancellationToken);

            return await tcs.Task;
        }

        public async Task YieldToUIAsync()
        {
            // Yield control to the UI thread to allow updates
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
        }
    }
}