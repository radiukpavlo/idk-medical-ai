using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MedicalAI.Core.Security;

namespace MedicalAI.Infrastructure.Security
{
    public class AuditLogger : IAuditLogger
    {
        private readonly ILogger<AuditLogger> _logger;
        private readonly string _auditLogPath;
        private readonly SemaphoreSlim _fileLock;
        private readonly List<AuditLogEntry> _memoryBuffer;
        private readonly Timer _flushTimer;

        public AuditLogger(ILogger<AuditLogger> logger)
        {
            _logger = logger;
            _auditLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "MedicalAI", "Audit", "audit.log");
            _fileLock = new SemaphoreSlim(1, 1);
            _memoryBuffer = new List<AuditLogEntry>();

            // Ensure audit directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(_auditLogPath)!);

            // Flush buffer every 30 seconds
            _flushTimer = new Timer(async _ => await FlushBufferAsync(), null, 
                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        public async Task LogDataAccessAsync(DataAccessEvent accessEvent, CancellationToken cancellationToken = default)
        {
            var logEntry = new AuditLogEntry(
                Guid.NewGuid(),
                accessEvent.Timestamp,
                "DATA_ACCESS",
                accessEvent.UserId,
                $"User accessed {accessEvent.Action} on resource",
                accessEvent.ResourcePath,
                JsonSerializer.Serialize(accessEvent));

            await AddLogEntryAsync(logEntry, cancellationToken);

            _logger.LogInformation("Data access logged: User {UserId} performed {Action} on {Resource}", 
                accessEvent.UserId, accessEvent.Action, accessEvent.ResourcePath);
        }

        public async Task LogSecurityEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
        {
            var logEntry = new AuditLogEntry(
                Guid.NewGuid(),
                securityEvent.Timestamp,
                securityEvent.EventType,
                securityEvent.UserId ?? "SYSTEM",
                securityEvent.Description,
                securityEvent.ResourcePath,
                JsonSerializer.Serialize(securityEvent));

            await AddLogEntryAsync(logEntry, cancellationToken);

            var logLevel = securityEvent.Severity switch
            {
                SecurityEventSeverity.Critical => LogLevel.Critical,
                SecurityEventSeverity.High => LogLevel.Error,
                SecurityEventSeverity.Medium => LogLevel.Warning,
                SecurityEventSeverity.Low => LogLevel.Information,
                _ => LogLevel.Information
            };

            _logger.Log(logLevel, "Security event logged: {EventType} - {Description}", 
                securityEvent.EventType, securityEvent.Description);
        }

        public async Task<IEnumerable<AuditLogEntry>> GetAuditLogsAsync(
            DateTime fromDate,
            DateTime toDate,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Retrieving audit logs from {FromDate} to {ToDate}", fromDate, toDate);

            await _fileLock.WaitAsync(cancellationToken);
            try
            {
                // First, flush any pending entries
                await FlushBufferInternalAsync();

                var logs = new List<AuditLogEntry>();

                if (File.Exists(_auditLogPath))
                {
                    var lines = await File.ReadAllLinesAsync(_auditLogPath, cancellationToken);
                    
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        try
                        {
                            var logEntry = JsonSerializer.Deserialize<AuditLogEntry>(line);
                            if (logEntry != null && 
                                logEntry.Timestamp >= fromDate && 
                                logEntry.Timestamp <= toDate)
                            {
                                logs.Add(logEntry);
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Failed to deserialize audit log entry: {Line}", line);
                        }
                    }
                }

                _logger.LogInformation("Retrieved {LogCount} audit log entries", logs.Count);
                return logs.OrderBy(l => l.Timestamp);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task<string> ExportAuditLogsAsync(
            DateTime fromDate,
            DateTime toDate,
            string format = "json",
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Exporting audit logs from {FromDate} to {ToDate} in {Format} format", 
                fromDate, toDate, format);

            var logs = await GetAuditLogsAsync(fromDate, toDate, cancellationToken);
            var exportPath = Path.Combine(Path.GetDirectoryName(_auditLogPath)!, 
                $"audit_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{format}");

            switch (format.ToLowerInvariant())
            {
                case "json":
                    await ExportAsJsonAsync(logs, exportPath, cancellationToken);
                    break;
                case "csv":
                    await ExportAsCsvAsync(logs, exportPath, cancellationToken);
                    break;
                case "xml":
                    await ExportAsXmlAsync(logs, exportPath, cancellationToken);
                    break;
                default:
                    throw new ArgumentException($"Unsupported export format: {format}");
            }

            _logger.LogInformation("Audit logs exported to: {ExportPath}", exportPath);
            return exportPath;
        }

        private async Task AddLogEntryAsync(AuditLogEntry logEntry, CancellationToken cancellationToken)
        {
            await _fileLock.WaitAsync(cancellationToken);
            try
            {
                _memoryBuffer.Add(logEntry);

                // Flush immediately for critical events
                if (logEntry.EventType.Contains("CRITICAL") || logEntry.EventType.Contains("SECURITY_BREACH"))
                {
                    await FlushBufferInternalAsync();
                }
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private async Task FlushBufferAsync()
        {
            if (_memoryBuffer.Count == 0)
                return;

            await _fileLock.WaitAsync();
            try
            {
                await FlushBufferInternalAsync();
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private async Task FlushBufferInternalAsync()
        {
            if (_memoryBuffer.Count == 0)
                return;

            try
            {
                var lines = _memoryBuffer.Select(entry => JsonSerializer.Serialize(entry));
                await File.AppendAllLinesAsync(_auditLogPath, lines);
                
                _logger.LogDebug("Flushed {EntryCount} audit log entries to disk", _memoryBuffer.Count);
                _memoryBuffer.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to flush audit log buffer to disk");
            }
        }

        private static async Task ExportAsJsonAsync(IEnumerable<AuditLogEntry> logs, string exportPath, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(exportPath, json, cancellationToken);
        }

        private static async Task ExportAsCsvAsync(IEnumerable<AuditLogEntry> logs, string exportPath, CancellationToken cancellationToken)
        {
            var lines = new List<string>
            {
                "Id,Timestamp,EventType,UserId,Description,ResourcePath,AdditionalData"
            };

            lines.AddRange(logs.Select(log => 
                $"{log.Id},{log.Timestamp:yyyy-MM-dd HH:mm:ss},{EscapeCsv(log.EventType)},{EscapeCsv(log.UserId)},{EscapeCsv(log.Description)},{EscapeCsv(log.ResourcePath ?? "")},{EscapeCsv(log.AdditionalData ?? "")}"));

            await File.WriteAllLinesAsync(exportPath, lines, cancellationToken);
        }

        private static async Task ExportAsXmlAsync(IEnumerable<AuditLogEntry> logs, string exportPath, CancellationToken cancellationToken)
        {
            var xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<AuditLogs>\n";
            
            foreach (var log in logs)
            {
                xml += $"  <LogEntry>\n";
                xml += $"    <Id>{log.Id}</Id>\n";
                xml += $"    <Timestamp>{log.Timestamp:yyyy-MM-dd HH:mm:ss}</Timestamp>\n";
                xml += $"    <EventType>{EscapeXml(log.EventType)}</EventType>\n";
                xml += $"    <UserId>{EscapeXml(log.UserId)}</UserId>\n";
                xml += $"    <Description>{EscapeXml(log.Description)}</Description>\n";
                xml += $"    <ResourcePath>{EscapeXml(log.ResourcePath ?? "")}</ResourcePath>\n";
                xml += $"    <AdditionalData>{EscapeXml(log.AdditionalData ?? "")}</AdditionalData>\n";
                xml += $"  </LogEntry>\n";
            }
            
            xml += "</AuditLogs>";
            await File.WriteAllTextAsync(exportPath, xml, cancellationToken);
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }

        private static string EscapeXml(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        public void Dispose()
        {
            _flushTimer?.Dispose();
            FlushBufferAsync().Wait();
            _fileLock?.Dispose();
        }
    }
}