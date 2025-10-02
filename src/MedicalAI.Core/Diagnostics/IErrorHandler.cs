using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MedicalAI.Core.Diagnostics
{
    /// <summary>
    /// Interface for comprehensive error handling and recovery
    /// </summary>
    public interface IErrorHandler
    {
        /// <summary>
        /// Handles an exception with context and recovery options
        /// </summary>
        Task<ErrorHandlingResult> HandleErrorAsync(
            Exception exception,
            ErrorContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Registers an error recovery strategy for a specific error type
        /// </summary>
        void RegisterRecoveryStrategy<TException>(IErrorRecoveryStrategy<TException> strategy)
            where TException : Exception;

        /// <summary>
        /// Gets user-friendly error message for an exception
        /// </summary>
        string GetUserFriendlyMessage(Exception exception, ErrorContext context);

        /// <summary>
        /// Determines if an error is recoverable
        /// </summary>
        bool IsRecoverable(Exception exception, ErrorContext context);
    }

    /// <summary>
    /// Interface for error recovery strategies
    /// </summary>
    public interface IErrorRecoveryStrategy<in TException> where TException : Exception
    {
        /// <summary>
        /// Attempts to recover from the specified exception
        /// </summary>
        Task<RecoveryResult> TryRecoverAsync(TException exception, ErrorContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Determines if this strategy can handle the exception
        /// </summary>
        bool CanHandle(TException exception, ErrorContext context);
    }

    /// <summary>
    /// Interface for diagnostic and troubleshooting tools
    /// </summary>
    public interface IDiagnosticService
    {
        /// <summary>
        /// Performs system health check
        /// </summary>
        Task<SystemHealthReport> PerformHealthCheckAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Collects diagnostic information for troubleshooting
        /// </summary>
        Task<DiagnosticReport> CollectDiagnosticsAsync(
            DiagnosticLevel level = DiagnosticLevel.Standard,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates troubleshooting suggestions for an error
        /// </summary>
        Task<IEnumerable<TroubleshootingSuggestion>> GetTroubleshootingSuggestionsAsync(
            Exception exception,
            ErrorContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Exports diagnostic data for support
        /// </summary>
        Task<string> ExportDiagnosticsAsync(
            DiagnosticReport report,
            string format = "json",
            CancellationToken cancellationToken = default);
    }

    // Data models
    public record ErrorContext(
        string OperationName,
        string? UserId = null,
        string? ResourcePath = null,
        Dictionary<string, object>? AdditionalData = null);

    public record ErrorHandlingResult(
        bool WasHandled,
        bool WasRecovered,
        string UserMessage,
        string? RecoveryAction = null,
        Exception? OriginalException = null);

    public record RecoveryResult(
        bool WasSuccessful,
        string? RecoveryAction = null,
        string? Message = null);

    public record SystemHealthReport(
        DateTime Timestamp,
        HealthStatus OverallStatus,
        IEnumerable<ComponentHealth> ComponentStatuses,
        IEnumerable<string> Issues,
        IEnumerable<string> Recommendations);

    public record ComponentHealth(
        string ComponentName,
        HealthStatus Status,
        string? Message = null,
        Dictionary<string, object>? Metrics = null);

    public record DiagnosticReport(
        DateTime Timestamp,
        DiagnosticLevel Level,
        SystemInfo SystemInfo,
        ApplicationInfo ApplicationInfo,
        IEnumerable<LogEntry> RecentLogs,
        IEnumerable<PerformanceMetric> PerformanceMetrics,
        IEnumerable<string> ConfigurationIssues);

    public record SystemInfo(
        string OperatingSystem,
        string Architecture,
        long TotalMemory,
        long AvailableMemory,
        int ProcessorCount,
        string DotNetVersion);

    public record ApplicationInfo(
        string Version,
        DateTime StartTime,
        TimeSpan Uptime,
        long WorkingSet,
        int ThreadCount,
        IEnumerable<string> LoadedAssemblies);

    public record LogEntry(
        DateTime Timestamp,
        string Level,
        string Message,
        string? Exception = null,
        string? Category = null);

    public record PerformanceMetric(
        string Name,
        double Value,
        string Unit,
        DateTime Timestamp);

    public record TroubleshootingSuggestion(
        string Title,
        string Description,
        TroubleshootingPriority Priority,
        IEnumerable<string> Steps);

    public enum HealthStatus
    {
        Healthy,
        Warning,
        Critical,
        Unknown
    }

    public enum DiagnosticLevel
    {
        Basic,
        Standard,
        Detailed,
        Comprehensive
    }

    public enum TroubleshootingPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}