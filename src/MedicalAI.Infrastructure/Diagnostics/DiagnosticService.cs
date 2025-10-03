using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MedicalAI.Core.Diagnostics;
using MedicalAI.Core.Performance;

namespace MedicalAI.Infrastructure.Diagnostics
{
    public class DiagnosticService : IDiagnosticService
    {
        private readonly ILogger<DiagnosticService> _logger;
        private readonly IMemoryManager _memoryManager;
        private readonly List<LogEntry> _recentLogs;
        private readonly List<PerformanceMetric> _performanceMetrics;
        private readonly object _lockObject = new();

        public DiagnosticService(ILogger<DiagnosticService> logger, IMemoryManager memoryManager)
        {
            _logger = logger;
            _memoryManager = memoryManager;
            _recentLogs = new List<LogEntry>();
            _performanceMetrics = new List<PerformanceMetric>();
        }

        public async Task<SystemHealthReport> PerformHealthCheckAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Performing system health check");

            var componentStatuses = new List<ComponentHealth>();
            var issues = new List<string>();
            var recommendations = new List<string>();

            // Check memory health
            var memoryHealth = await CheckMemoryHealthAsync(cancellationToken);
            componentStatuses.Add(memoryHealth);
            if (memoryHealth.Status != HealthStatus.Healthy)
            {
                issues.Add($"Memory: {memoryHealth.Message}");
                if (memoryHealth.Status == HealthStatus.Critical)
                {
                    recommendations.Add("Consider closing other applications to free up memory");
                }
            }

            // Check disk space
            var diskHealth = CheckDiskHealth();
            componentStatuses.Add(diskHealth);
            if (diskHealth.Status != HealthStatus.Healthy)
            {
                issues.Add($"Disk: {diskHealth.Message}");
                if (diskHealth.Status == HealthStatus.Critical)
                {
                    recommendations.Add("Free up disk space by removing unnecessary files");
                }
            }

            // Check application health
            var appHealth = CheckApplicationHealth();
            componentStatuses.Add(appHealth);
            if (appHealth.Status != HealthStatus.Healthy)
            {
                issues.Add($"Application: {appHealth.Message}");
            }

            // Check file system permissions
            var permissionHealth = await CheckFileSystemPermissionsAsync(cancellationToken);
            componentStatuses.Add(permissionHealth);
            if (permissionHealth.Status != HealthStatus.Healthy)
            {
                issues.Add($"Permissions: {permissionHealth.Message}");
                recommendations.Add("Check file and folder permissions for the application");
            }

            var overallStatus = DetermineOverallHealth(componentStatuses);

            _logger.LogInformation("Health check completed. Overall status: {OverallStatus}, Issues: {IssueCount}", 
                overallStatus, issues.Count);

            return new SystemHealthReport(
                DateTime.UtcNow,
                overallStatus,
                componentStatuses,
                issues,
                recommendations);
        }

        public async Task<DiagnosticReport> CollectDiagnosticsAsync(
            DiagnosticLevel level = DiagnosticLevel.Standard,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Collecting diagnostics at level: {Level}", level);

            var systemInfo = CollectSystemInfo();
            var applicationInfo = CollectApplicationInfo();
            var configurationIssues = await CheckConfigurationAsync(cancellationToken);

            var recentLogs = level >= DiagnosticLevel.Standard ? GetRecentLogs(100) : GetRecentLogs(20);
            var performanceMetrics = level >= DiagnosticLevel.Detailed ? GetPerformanceMetrics(50) : GetPerformanceMetrics(10);

            if (level >= DiagnosticLevel.Comprehensive)
            {
                // Collect additional detailed information
                await CollectDetailedDiagnosticsAsync(cancellationToken);
            }

            var report = new DiagnosticReport(
                DateTime.UtcNow,
                level,
                systemInfo,
                applicationInfo,
                recentLogs,
                performanceMetrics,
                configurationIssues);

            _logger.LogInformation("Diagnostic collection completed");
            return report;
        }

        public async Task<IEnumerable<TroubleshootingSuggestion>> GetTroubleshootingSuggestionsAsync(
            Exception exception,
            ErrorContext context,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Generating troubleshooting suggestions for exception: {ExceptionType}", exception.GetType().Name);

            var suggestions = new List<TroubleshootingSuggestion>();

            // Generate suggestions based on exception type
            suggestions.AddRange(GetExceptionSpecificSuggestions(exception));

            // Generate suggestions based on context
            suggestions.AddRange(GetContextSpecificSuggestions(context));

            // Generate suggestions based on system state
            var systemSuggestions = await GetSystemStateSuggestionsAsync(cancellationToken);
            suggestions.AddRange(systemSuggestions);

            // Sort by priority
            return suggestions.OrderByDescending(s => s.Priority);
        }

        public async Task<string> ExportDiagnosticsAsync(
            DiagnosticReport report,
            string format = "json",
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Exporting diagnostics in {Format} format", format);

            var exportPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MedicalAI", "Diagnostics", 
                $"diagnostics_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{format}");

            Directory.CreateDirectory(Path.GetDirectoryName(exportPath)!);

            switch (format.ToLowerInvariant())
            {
                case "json":
                    await ExportAsJsonAsync(report, exportPath, cancellationToken);
                    break;
                case "txt":
                    await ExportAsTextAsync(report, exportPath, cancellationToken);
                    break;
                case "xml":
                    await ExportAsXmlAsync(report, exportPath, cancellationToken);
                    break;
                default:
                    throw new ArgumentException($"Unsupported export format: {format}");
            }

            _logger.LogInformation("Diagnostics exported to: {ExportPath}", exportPath);
            return exportPath;
        }

        public void LogEntry(LogEntry entry)
        {
            lock (_lockObject)
            {
                _recentLogs.Add(entry);
                if (_recentLogs.Count > 1000) // Keep only recent logs
                {
                    _recentLogs.RemoveAt(0);
                }
            }
        }

        public void RecordPerformanceMetric(PerformanceMetric metric)
        {
            lock (_lockObject)
            {
                _performanceMetrics.Add(metric);
                if (_performanceMetrics.Count > 500) // Keep only recent metrics
                {
                    _performanceMetrics.RemoveAt(0);
                }
            }
        }

        private Task<ComponentHealth> CheckMemoryHealthAsync(CancellationToken cancellationToken)
        {
            var currentUsage = _memoryManager.GetCurrentMemoryUsage();
            var availableMemory = _memoryManager.GetAvailableMemory();
            var usagePercentage = (double)currentUsage / availableMemory * 100;

            var metrics = new Dictionary<string, object>
            {
                ["CurrentUsage"] = currentUsage,
                ["AvailableMemory"] = availableMemory,
                ["UsagePercentage"] = usagePercentage
            };

            var status = usagePercentage switch
            {
                < 70 => HealthStatus.Healthy,
                < 85 => HealthStatus.Warning,
                _ => HealthStatus.Critical
            };

            var message = status switch
            {
                HealthStatus.Healthy => "Memory usage is normal",
                HealthStatus.Warning => $"Memory usage is high ({usagePercentage:F1}%)",
                HealthStatus.Critical => $"Memory usage is critical ({usagePercentage:F1}%)",
                _ => "Memory status unknown"
            };

            var health = new ComponentHealth("Memory", status, message, metrics);
            return Task.FromResult(health);
        }

        private ComponentHealth CheckDiskHealth()
        {
            try
            {
                var drive = new DriveInfo(Environment.CurrentDirectory);
                var freeSpaceGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                var totalSpaceGB = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                var usagePercentage = (1 - (double)drive.AvailableFreeSpace / drive.TotalSize) * 100;

                var metrics = new Dictionary<string, object>
                {
                    ["FreeSpaceGB"] = freeSpaceGB,
                    ["TotalSpaceGB"] = totalSpaceGB,
                    ["UsagePercentage"] = usagePercentage
                };

                var status = freeSpaceGB switch
                {
                    > 5 => HealthStatus.Healthy,
                    > 1 => HealthStatus.Warning,
                    _ => HealthStatus.Critical
                };

                var message = status switch
                {
                    HealthStatus.Healthy => $"Disk space is adequate ({freeSpaceGB:F1} GB free)",
                    HealthStatus.Warning => $"Disk space is low ({freeSpaceGB:F1} GB free)",
                    HealthStatus.Critical => $"Disk space is critically low ({freeSpaceGB:F1} GB free)",
                    _ => "Disk status unknown"
                };

                return new ComponentHealth("Disk", status, message, metrics);
            }
            catch (Exception ex)
            {
                return new ComponentHealth("Disk", HealthStatus.Unknown, $"Could not check disk space: {ex.Message}");
            }
        }

        private ComponentHealth CheckApplicationHealth()
        {
            var process = Process.GetCurrentProcess();
            var uptime = DateTime.Now - process.StartTime;
            var threadCount = process.Threads.Count;
            var workingSet = process.WorkingSet64;

            var metrics = new Dictionary<string, object>
            {
                ["Uptime"] = uptime.TotalMinutes,
                ["ThreadCount"] = threadCount,
                ["WorkingSetMB"] = workingSet / (1024.0 * 1024.0)
            };

            var status = threadCount switch
            {
                < 50 => HealthStatus.Healthy,
                < 100 => HealthStatus.Warning,
                _ => HealthStatus.Critical
            };

            var message = status switch
            {
                HealthStatus.Healthy => "Application is running normally",
                HealthStatus.Warning => $"High thread count ({threadCount})",
                HealthStatus.Critical => $"Very high thread count ({threadCount})",
                _ => "Application status unknown"
            };

            return new ComponentHealth("Application", status, message, metrics);
        }

        private async Task<ComponentHealth> CheckFileSystemPermissionsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var testPath = Path.Combine(Path.GetTempPath(), "medicalai_permission_test.tmp");
                await File.WriteAllTextAsync(testPath, "test", cancellationToken);
                File.Delete(testPath);

                return new ComponentHealth("FileSystem", HealthStatus.Healthy, "File system permissions are adequate");
            }
            catch (UnauthorizedAccessException)
            {
                return new ComponentHealth("FileSystem", HealthStatus.Critical, "Insufficient file system permissions");
            }
            catch (Exception ex)
            {
                return new ComponentHealth("FileSystem", HealthStatus.Warning, $"Could not verify permissions: {ex.Message}");
            }
        }

        private SystemInfo CollectSystemInfo()
        {
            var process = Process.GetCurrentProcess();
            return new SystemInfo(
                RuntimeInformation.OSDescription,
                RuntimeInformation.OSArchitecture.ToString(),
                GC.GetTotalMemory(false),
                _memoryManager.GetAvailableMemory(),
                Environment.ProcessorCount,
                RuntimeInformation.FrameworkDescription);
        }

        private ApplicationInfo CollectApplicationInfo()
        {
            var process = Process.GetCurrentProcess();
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "Unknown";
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetName().Name ?? "Unknown")
                .Where(name => name != "Unknown")
                .ToList();

            return new ApplicationInfo(
                version,
                process.StartTime,
                DateTime.Now - process.StartTime,
                process.WorkingSet64,
                process.Threads.Count,
                loadedAssemblies);
        }

        private Task<IEnumerable<string>> CheckConfigurationAsync(CancellationToken cancellationToken)
        {
            var issues = new List<string>();

            // Check if required directories exist
            var requiredDirs = new[] { "models", "datasets", "temp" };
            foreach (var dir in requiredDirs)
            {
                if (!Directory.Exists(dir))
                {
                    issues.Add($"Required directory missing: {dir}");
                }
            }

            // Check configuration files
            var configFiles = new[] { "appsettings.json", "logging.json" };
            foreach (var file in configFiles)
            {
                if (!File.Exists(file))
                {
                    issues.Add($"Configuration file missing: {file}");
                }
            }

            return Task.FromResult<IEnumerable<string>>(issues);
        }

        private IEnumerable<LogEntry> GetRecentLogs(int count)
        {
            lock (_lockObject)
            {
                return _recentLogs.TakeLast(count).ToList();
            }
        }

        private IEnumerable<PerformanceMetric> GetPerformanceMetrics(int count)
        {
            lock (_lockObject)
            {
                return _performanceMetrics.TakeLast(count).ToList();
            }
        }

        private async Task CollectDetailedDiagnosticsAsync(CancellationToken cancellationToken)
        {
            // Collect additional detailed information for comprehensive diagnostics
            // This could include network connectivity, plugin status, etc.
            await Task.Delay(100, cancellationToken); // Placeholder
        }

        private IEnumerable<TroubleshootingSuggestion> GetExceptionSpecificSuggestions(Exception exception)
        {
            return exception switch
            {
                FileNotFoundException => new[]
                {
                    new TroubleshootingSuggestion(
                        "File Not Found",
                        "The requested file could not be located",
                        TroubleshootingPriority.High,
                        new[] { "Check if the file path is correct", "Verify the file exists", "Check file permissions" })
                },
                OutOfMemoryException => new[]
                {
                    new TroubleshootingSuggestion(
                        "Memory Issues",
                        "The application is running out of memory",
                        TroubleshootingPriority.Critical,
                        new[] { "Close other applications", "Restart the application", "Process smaller files", "Check for memory leaks" })
                },
                UnauthorizedAccessException => new[]
                {
                    new TroubleshootingSuggestion(
                        "Permission Issues",
                        "Access to the resource is denied",
                        TroubleshootingPriority.High,
                        new[] { "Run as administrator", "Check file permissions", "Verify user access rights" })
                },
                _ => Array.Empty<TroubleshootingSuggestion>()
            };
        }

        private IEnumerable<TroubleshootingSuggestion> GetContextSpecificSuggestions(ErrorContext context)
        {
            var suggestions = new List<TroubleshootingSuggestion>();

            if (context.OperationName.Contains("DICOM", StringComparison.OrdinalIgnoreCase))
            {
                suggestions.Add(new TroubleshootingSuggestion(
                    "DICOM Processing Issues",
                    "Problems with DICOM file processing",
                    TroubleshootingPriority.Medium,
                    new[] { "Verify DICOM file integrity", "Check file format compliance", "Ensure sufficient memory" }));
            }

            if (context.ResourcePath != null && context.ResourcePath.Contains("network"))
            {
                suggestions.Add(new TroubleshootingSuggestion(
                    "Network Issues",
                    "Network-related operation failed",
                    TroubleshootingPriority.Medium,
                    new[] { "Check network connectivity", "Verify firewall settings", "Try again later" }));
            }

            return suggestions;
        }

        private async Task<IEnumerable<TroubleshootingSuggestion>> GetSystemStateSuggestionsAsync(CancellationToken cancellationToken)
        {
            var suggestions = new List<TroubleshootingSuggestion>();
            var healthReport = await PerformHealthCheckAsync(cancellationToken);

            if (healthReport.OverallStatus == HealthStatus.Critical)
            {
                suggestions.Add(new TroubleshootingSuggestion(
                    "System Health Critical",
                    "The system is in a critical state",
                    TroubleshootingPriority.Critical,
                    new[] { "Restart the application", "Check system resources", "Contact support" }));
            }

            return suggestions;
        }

        private static HealthStatus DetermineOverallHealth(IEnumerable<ComponentHealth> componentStatuses)
        {
            var statuses = componentStatuses.Select(c => c.Status).ToList();
            
            if (statuses.Any(s => s == HealthStatus.Critical))
                return HealthStatus.Critical;
            if (statuses.Any(s => s == HealthStatus.Warning))
                return HealthStatus.Warning;
            if (statuses.Any(s => s == HealthStatus.Unknown))
                return HealthStatus.Unknown;
            
            return HealthStatus.Healthy;
        }

        private static async Task ExportAsJsonAsync(DiagnosticReport report, string exportPath, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(exportPath, json, cancellationToken);
        }

        private static async Task ExportAsTextAsync(DiagnosticReport report, string exportPath, CancellationToken cancellationToken)
        {
            var text = $"Diagnostic Report - {report.Timestamp:yyyy-MM-dd HH:mm:ss}\n";
            text += $"Level: {report.Level}\n\n";
            text += $"System Info:\n";
            text += $"  OS: {report.SystemInfo.OperatingSystem}\n";
            text += $"  Architecture: {report.SystemInfo.Architecture}\n";
            text += $"  Memory: {report.SystemInfo.TotalMemory / (1024 * 1024)} MB\n";
            text += $"  Processors: {report.SystemInfo.ProcessorCount}\n\n";
            
            text += $"Application Info:\n";
            text += $"  Version: {report.ApplicationInfo.Version}\n";
            text += $"  Uptime: {report.ApplicationInfo.Uptime}\n";
            text += $"  Working Set: {report.ApplicationInfo.WorkingSet / (1024 * 1024)} MB\n\n";
            
            if (report.ConfigurationIssues.Any())
            {
                text += "Configuration Issues:\n";
                foreach (var issue in report.ConfigurationIssues)
                {
                    text += $"  - {issue}\n";
                }
                text += "\n";
            }

            await File.WriteAllTextAsync(exportPath, text, cancellationToken);
        }

        private static async Task ExportAsXmlAsync(DiagnosticReport report, string exportPath, CancellationToken cancellationToken)
        {
            var xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n";
            xml += "<DiagnosticReport>\n";
            xml += $"  <Timestamp>{report.Timestamp:yyyy-MM-dd HH:mm:ss}</Timestamp>\n";
            xml += $"  <Level>{report.Level}</Level>\n";
            xml += "  <SystemInfo>\n";
            xml += $"    <OperatingSystem>{report.SystemInfo.OperatingSystem}</OperatingSystem>\n";
            xml += $"    <Architecture>{report.SystemInfo.Architecture}</Architecture>\n";
            xml += $"    <TotalMemory>{report.SystemInfo.TotalMemory}</TotalMemory>\n";
            xml += $"    <ProcessorCount>{report.SystemInfo.ProcessorCount}</ProcessorCount>\n";
            xml += "  </SystemInfo>\n";
            xml += "</DiagnosticReport>";

            await File.WriteAllTextAsync(exportPath, xml, cancellationToken);
        }
    }
}
