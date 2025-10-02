using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using MedicalAI.Core.Diagnostics;

namespace MedicalAI.Infrastructure.Diagnostics
{
    /// <summary>
    /// Enhanced logging service with structured logging and performance tracking
    /// </summary>
    public interface IStructuredLoggingService
    {
        /// <summary>
        /// Logs an operation with timing and context
        /// </summary>
        IDisposable LogOperation(string operationName, object? context = null, LogLevel logLevel = LogLevel.Information);

        /// <summary>
        /// Logs a performance metric
        /// </summary>
        void LogPerformanceMetric(string metricName, double value, string unit = "ms", object? context = null);

        /// <summary>
        /// Logs an error with full context and troubleshooting information
        /// </summary>
        void LogError(Exception exception, string message, object? context = null, [CallerMemberName] string? callerName = null);

        /// <summary>
        /// Logs a security event
        /// </summary>
        void LogSecurityEvent(string eventType, string description, object? context = null);

        /// <summary>
        /// Logs user activity for audit purposes
        /// </summary>
        void LogUserActivity(string userId, string activity, object? context = null);
    }

    public class StructuredLoggingService : IStructuredLoggingService
    {
        private readonly ILogger<StructuredLoggingService> _logger;
        private readonly DiagnosticService _diagnosticService;

        public StructuredLoggingService(ILogger<StructuredLoggingService> logger, DiagnosticService diagnosticService)
        {
            _logger = logger;
            _diagnosticService = diagnosticService;
        }

        public IDisposable LogOperation(string operationName, object? context = null, LogLevel logLevel = LogLevel.Information)
        {
            return new OperationLogger(_logger, _diagnosticService, operationName, context, logLevel);
        }

        public void LogPerformanceMetric(string metricName, double value, string unit = "ms", object? context = null)
        {
            var metric = new PerformanceMetric(metricName, value, unit, DateTime.UtcNow);
            _diagnosticService.RecordPerformanceMetric(metric);

            _logger.Log(LogLevel.Debug, "Performance metric recorded: {MetricName} = {Value} {Unit} {@Context}",
                metricName, value, unit, context);
        }

        public void LogError(Exception exception, string message, object? context = null, [CallerMemberName] string? callerName = null)
        {
            var logEntry = new LogEntry(
                DateTime.UtcNow,
                "Error",
                message,
                exception.ToString(),
                callerName);

            _diagnosticService.LogEntry(logEntry);

            _logger.LogError(exception, "{Message} in {CallerName} {@Context}", message, callerName, context);
        }

        public void LogSecurityEvent(string eventType, string description, object? context = null)
        {
            var logEntry = new LogEntry(
                DateTime.UtcNow,
                "Security",
                $"{eventType}: {description}",
                null,
                "Security");

            _diagnosticService.LogEntry(logEntry);

            _logger.LogWarning("Security event: {EventType} - {Description} {@Context}", eventType, description, context);
        }

        public void LogUserActivity(string userId, string activity, object? context = null)
        {
            var logEntry = new LogEntry(
                DateTime.UtcNow,
                "UserActivity",
                $"User {userId}: {activity}",
                null,
                "UserActivity");

            _diagnosticService.LogEntry(logEntry);

            _logger.LogInformation("User activity: {UserId} performed {Activity} {@Context}", userId, activity, context);
        }
    }

    /// <summary>
    /// Disposable operation logger that tracks timing and logs completion
    /// </summary>
    public class OperationLogger : IDisposable
    {
        private readonly ILogger _logger;
        private readonly DiagnosticService _diagnosticService;
        private readonly string _operationName;
        private readonly object? _context;
        private readonly LogLevel _logLevel;
        private readonly Stopwatch _stopwatch;
        private readonly DateTime _startTime;
        private bool _disposed;

        public OperationLogger(
            ILogger logger,
            DiagnosticService diagnosticService,
            string operationName,
            object? context,
            LogLevel logLevel)
        {
            _logger = logger;
            _diagnosticService = diagnosticService;
            _operationName = operationName;
            _context = context;
            _logLevel = logLevel;
            _stopwatch = Stopwatch.StartNew();
            _startTime = DateTime.UtcNow;

            _logger.Log(_logLevel, "Starting operation: {OperationName} {@Context}", _operationName, _context);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _stopwatch.Stop();
            var duration = _stopwatch.ElapsedMilliseconds;

            // Log completion
            _logger.Log(_logLevel, "Completed operation: {OperationName} in {Duration}ms {@Context}",
                _operationName, duration, _context);

            // Record performance metric
            var metric = new PerformanceMetric($"Operation.{_operationName}", duration, "ms", DateTime.UtcNow);
            _diagnosticService.RecordPerformanceMetric(metric);

            // Log entry for diagnostics
            var logEntry = new LogEntry(
                _startTime,
                _logLevel.ToString(),
                $"Operation {_operationName} completed in {duration}ms",
                null,
                "Operations");

            _diagnosticService.LogEntry(logEntry);

            _disposed = true;
        }
    }

    /// <summary>
    /// Extension methods for enhanced logging
    /// </summary>
    public static class LoggingExtensions
    {
        public static void LogMedicalDataAccess(this ILogger logger, string userId, string dataType, string action, string? resourcePath = null)
        {
            logger.LogInformation("Medical data access: User {UserId} performed {Action} on {DataType} {ResourcePath}",
                userId, action, dataType, resourcePath ?? "unknown");
        }

        public static void LogDicomProcessing(this ILogger logger, string operation, int fileCount, TimeSpan duration)
        {
            logger.LogInformation("DICOM processing: {Operation} completed for {FileCount} files in {Duration}",
                operation, fileCount, duration);
        }

        public static void LogAIModelInference(this ILogger logger, string modelName, string inputType, TimeSpan inferenceTime)
        {
            logger.LogInformation("AI model inference: {ModelName} processed {InputType} in {InferenceTime}",
                modelName, inputType, inferenceTime);
        }

        public static void LogMemoryUsage(this ILogger logger, long currentUsage, long availableMemory)
        {
            var usagePercentage = (double)currentUsage / availableMemory * 100;
            var logLevel = usagePercentage > 80 ? LogLevel.Warning : LogLevel.Debug;
            
            logger.Log(logLevel, "Memory usage: {CurrentUsage} MB / {AvailableMemory} MB ({UsagePercentage:F1}%)",
                currentUsage / (1024 * 1024), availableMemory / (1024 * 1024), usagePercentage);
        }

        public static void LogFileOperation(this ILogger logger, string operation, string filePath, long fileSize, bool success)
        {
            var logLevel = success ? LogLevel.Information : LogLevel.Warning;
            logger.Log(logLevel, "File operation: {Operation} on {FilePath} ({FileSize} bytes) - {Result}",
                operation, filePath, fileSize, success ? "Success" : "Failed");
        }

        public static void LogPluginActivity(this ILogger logger, string pluginName, string activity, bool success, string? errorMessage = null)
        {
            if (success)
            {
                logger.LogInformation("Plugin activity: {PluginName} - {Activity} completed successfully", pluginName, activity);
            }
            else
            {
                logger.LogError("Plugin activity: {PluginName} - {Activity} failed: {ErrorMessage}", 
                    pluginName, activity, errorMessage ?? "Unknown error");
            }
        }
    }
}