using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security;
using Microsoft.Extensions.Logging;
using MedicalAI.Core.Diagnostics;
using MedicalAI.Core.Security;

namespace MedicalAI.Infrastructure.Diagnostics
{
    public class ErrorHandler : IErrorHandler
    {
        private readonly ILogger<ErrorHandler> _logger;
        private readonly IAuditLogger _auditLogger;
        private readonly ConcurrentDictionary<Type, object> _recoveryStrategies;

        public ErrorHandler(ILogger<ErrorHandler> logger, IAuditLogger auditLogger)
        {
            _logger = logger;
            _auditLogger = auditLogger;
            _recoveryStrategies = new ConcurrentDictionary<Type, object>();
            
            // Register default recovery strategies
            RegisterDefaultStrategies();
        }

        public async Task<ErrorHandlingResult> HandleErrorAsync(
            Exception exception,
            ErrorContext context,
            CancellationToken cancellationToken = default)
        {
            _logger.LogError(exception, "Handling error in operation: {OperationName}", context.OperationName);

            // Log security event for audit trail
            await _auditLogger.LogSecurityEventAsync(
                new SecurityEvent(
                    "ERROR_OCCURRED",
                    $"Error in {context.OperationName}: {exception.Message}",
                    DateTime.UtcNow,
                    context.UserId,
                    context.ResourcePath,
                    GetSecuritySeverity(exception)),
                cancellationToken);

            var userMessage = GetUserFriendlyMessage(exception, context);
            var isRecoverable = IsRecoverable(exception, context);

            if (isRecoverable)
            {
                var recoveryResult = await TryRecoverAsync(exception, context, cancellationToken);
                if (recoveryResult.WasSuccessful)
                {
                    _logger.LogInformation("Successfully recovered from error in operation: {OperationName}", context.OperationName);
                    return new ErrorHandlingResult(true, true, userMessage, recoveryResult.RecoveryAction, exception);
                }
            }

            return new ErrorHandlingResult(true, false, userMessage, null, exception);
        }

        public void RegisterRecoveryStrategy<TException>(IErrorRecoveryStrategy<TException> strategy)
            where TException : Exception
        {
            _recoveryStrategies[typeof(TException)] = strategy;
            _logger.LogDebug("Registered recovery strategy for exception type: {ExceptionType}", typeof(TException).Name);
        }

        public string GetUserFriendlyMessage(Exception exception, ErrorContext context)
        {
            return exception switch
            {
                FileNotFoundException => "The requested file could not be found. Please check the file path and try again.",
                UnauthorizedAccessException => "Access denied. You don't have permission to perform this operation.",
                OutOfMemoryException => "The system is running low on memory. Please close other applications and try again.",
                TimeoutException => "The operation timed out. Please check your network connection and try again.",
                ArgumentException => "Invalid input provided. Please check your data and try again.",
                InvalidOperationException => "This operation cannot be performed at this time. Please try again later.",
                NotSupportedException => "This operation is not supported with the current configuration.",
                IOException => "A file system error occurred. Please check disk space and file permissions.",
                _ => GetGenericUserMessage(exception, context)
            };
        }

        public bool IsRecoverable(Exception exception, ErrorContext context)
        {
            return exception switch
            {
                OutOfMemoryException => true,
                TimeoutException => true,
                IOException when !exception.Message.Contains("disk full") => true,
                UnauthorizedAccessException => false,
                ArgumentNullException => false,
                NotSupportedException => false,
                _ => _recoveryStrategies.ContainsKey(exception.GetType())
            };
        }

        private async Task<RecoveryResult> TryRecoverAsync(
            Exception exception,
            ErrorContext context,
            CancellationToken cancellationToken)
        {
            var exceptionType = exception.GetType();
            
            // Try exact type match first
            if (_recoveryStrategies.TryGetValue(exceptionType, out var strategy))
            {
                return await ExecuteRecoveryStrategy(strategy, exception, context, cancellationToken);
            }

            // Try base type matches
            var baseType = exceptionType.BaseType;
            while (baseType != null && baseType != typeof(Exception))
            {
                if (_recoveryStrategies.TryGetValue(baseType, out strategy))
                {
                    return await ExecuteRecoveryStrategy(strategy, exception, context, cancellationToken);
                }
                baseType = baseType.BaseType;
            }

            // Try built-in recovery strategies
            return await TryBuiltInRecovery(exception, context, cancellationToken);
        }

        private async Task<RecoveryResult> ExecuteRecoveryStrategy(
            object strategy,
            Exception exception,
            ErrorContext context,
            CancellationToken cancellationToken)
        {
            try
            {
                var method = strategy.GetType().GetMethod("TryRecoverAsync");
                if (method != null)
                {
                    var task = (Task<RecoveryResult>)method.Invoke(strategy, new object[] { exception, context, cancellationToken })!;
                    return await task;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Recovery strategy failed for exception type: {ExceptionType}", exception.GetType().Name);
            }

            return new RecoveryResult(false, null, "Recovery strategy execution failed");
        }

        private async Task<RecoveryResult> TryBuiltInRecovery(
            Exception exception,
            ErrorContext context,
            CancellationToken cancellationToken)
        {
            return exception switch
            {
                OutOfMemoryException => await RecoverFromOutOfMemory(cancellationToken),
                TimeoutException => await RecoverFromTimeout(context, cancellationToken),
                IOException ioEx => await RecoverFromIOException(ioEx, context, cancellationToken),
                _ => new RecoveryResult(false, null, "No built-in recovery available")
            };
        }

        private async Task<RecoveryResult> RecoverFromOutOfMemory(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Attempting to recover from OutOfMemoryException");
            
            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            // Wait a bit for cleanup
            await Task.Delay(1000, cancellationToken);
            
            return new RecoveryResult(true, "Memory cleanup", "Performed garbage collection and memory cleanup");
        }

        private async Task<RecoveryResult> RecoverFromTimeout(ErrorContext context, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Attempting to recover from TimeoutException in operation: {OperationName}", context.OperationName);
            
            // Wait before retry
            await Task.Delay(2000, cancellationToken);
            
            return new RecoveryResult(true, "Retry after delay", "Waited and prepared for retry");
        }

        private async Task<RecoveryResult> RecoverFromIOException(IOException ioException, ErrorContext context, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Attempting to recover from IOException: {Message}", ioException.Message);
            
            if (context.ResourcePath != null)
            {
                try
                {
                    // Ensure directory exists
                    var directory = Path.GetDirectoryName(context.ResourcePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                        return new RecoveryResult(true, "Created missing directory", $"Created directory: {directory}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create directory for recovery");
                }
            }
            
            await Task.Delay(500, cancellationToken);
            return new RecoveryResult(true, "Retry IO operation", "Prepared for IO retry");
        }

        private string GetGenericUserMessage(Exception exception, ErrorContext context)
        {
            var operation = context.OperationName.ToLowerInvariant();
            
            return operation switch
            {
                var op when op.Contains("load") || op.Contains("read") => 
                    "Failed to load the requested data. Please check the file and try again.",
                var op when op.Contains("save") || op.Contains("write") => 
                    "Failed to save the data. Please check disk space and permissions.",
                var op when op.Contains("process") || op.Contains("analyze") => 
                    "Failed to process the data. Please verify the input and try again.",
                var op when op.Contains("connect") || op.Contains("network") => 
                    "Connection failed. Please check your network connection and try again.",
                _ => "An unexpected error occurred. Please try again or contact support if the problem persists."
            };
        }

        private static SecurityEventSeverity GetSecuritySeverity(Exception exception)
        {
            return exception switch
            {
                UnauthorizedAccessException => SecurityEventSeverity.High,
                SecurityException => SecurityEventSeverity.High,
                OutOfMemoryException => SecurityEventSeverity.Medium,
                _ => SecurityEventSeverity.Low
            };
        }

        private void RegisterDefaultStrategies()
        {
            // Register common recovery strategies
            RegisterRecoveryStrategy(new FileNotFoundRecoveryStrategy(_logger));
            RegisterRecoveryStrategy(new NetworkTimeoutRecoveryStrategy(_logger));
        }
    }

    // Built-in recovery strategies
    public class FileNotFoundRecoveryStrategy : IErrorRecoveryStrategy<FileNotFoundException>
    {
        private readonly ILogger _logger;

        public FileNotFoundRecoveryStrategy(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<RecoveryResult> TryRecoverAsync(
            FileNotFoundException exception,
            ErrorContext context,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Attempting to recover from FileNotFoundException: {FileName}", exception.FileName);

            if (context.ResourcePath != null)
            {
                // Try to find the file in common locations
                var fileName = Path.GetFileName(context.ResourcePath);
                var searchPaths = new[]
                {
                    Environment.CurrentDirectory,
                    Path.Combine(Environment.CurrentDirectory, "data"),
                    Path.Combine(Environment.CurrentDirectory, "datasets"),
                    Path.Combine(Environment.CurrentDirectory, "models")
                };

                foreach (var searchPath in searchPaths)
                {
                    var candidatePath = Path.Combine(searchPath, fileName);
                    if (File.Exists(candidatePath))
                    {
                        _logger.LogInformation("Found file at alternative location: {CandidatePath}", candidatePath);
                        return new RecoveryResult(true, $"Use alternative path: {candidatePath}", 
                            "Found file in alternative location");
                    }
                }
            }

            await Task.Delay(100, cancellationToken); // Minimal delay
            return new RecoveryResult(false, null, "File not found in any known location");
        }

        public bool CanHandle(FileNotFoundException exception, ErrorContext context)
        {
            return !string.IsNullOrEmpty(context.ResourcePath);
        }
    }

    public class NetworkTimeoutRecoveryStrategy : IErrorRecoveryStrategy<TimeoutException>
    {
        private readonly ILogger _logger;
        private int _retryCount = 0;
        private const int MaxRetries = 3;

        public NetworkTimeoutRecoveryStrategy(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<RecoveryResult> TryRecoverAsync(
            TimeoutException exception,
            ErrorContext context,
            CancellationToken cancellationToken)
        {
            _retryCount++;
            _logger.LogInformation("Attempting network timeout recovery, attempt {RetryCount}/{MaxRetries}", _retryCount, MaxRetries);

            if (_retryCount > MaxRetries)
            {
                _retryCount = 0;
                return new RecoveryResult(false, null, "Maximum retry attempts exceeded");
            }

            // Exponential backoff
            var delay = TimeSpan.FromSeconds(Math.Pow(2, _retryCount));
            await Task.Delay(delay, cancellationToken);

            return new RecoveryResult(true, $"Retry with backoff (attempt {_retryCount})", 
                $"Waiting {delay.TotalSeconds} seconds before retry");
        }

        public bool CanHandle(TimeoutException exception, ErrorContext context)
        {
            return context.OperationName.Contains("network", StringComparison.OrdinalIgnoreCase) ||
                   context.OperationName.Contains("download", StringComparison.OrdinalIgnoreCase) ||
                   context.OperationName.Contains("upload", StringComparison.OrdinalIgnoreCase);
        }
    }
}
