using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using MedicalAI.Core.Diagnostics;
using MedicalAI.Core.Security;

namespace MedicalAI.UI.Services
{
    /// <summary>
    /// Global exception handler for the UI application
    /// </summary>
    public interface IGlobalExceptionHandler
    {
        /// <summary>
        /// Handles unhandled exceptions in the UI
        /// </summary>
        Task HandleUnhandledExceptionAsync(Exception exception, string? context = null);

        /// <summary>
        /// Handles exceptions with user notification
        /// </summary>
        Task<bool> HandleExceptionWithUserNotificationAsync(Exception exception, string operationName, string? userId = null);
    }

    public class GlobalExceptionHandler : IGlobalExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IErrorHandler _errorHandler;
        private readonly IDiagnosticService _diagnosticService;
        private readonly IAuditLogger _auditLogger;

        public GlobalExceptionHandler(
            ILogger<GlobalExceptionHandler> logger,
            IErrorHandler errorHandler,
            IDiagnosticService diagnosticService,
            IAuditLogger auditLogger)
        {
            _logger = logger;
            _errorHandler = errorHandler;
            _diagnosticService = diagnosticService;
            _auditLogger = auditLogger;
        }

        public async Task HandleUnhandledExceptionAsync(Exception exception, string? context = null)
        {
            _logger.LogCritical(exception, "Unhandled exception occurred in context: {Context}", context ?? "Unknown");

            try
            {
                // Log security event for unhandled exceptions
                await _auditLogger.LogSecurityEventAsync(
                    new SecurityEvent(
                        "UNHANDLED_EXCEPTION",
                        $"Unhandled exception in {context}: {exception.Message}",
                        DateTime.UtcNow,
                        "SYSTEM",
                        null,
                        SecurityEventSeverity.Critical),
                    CancellationToken.None);

                // Collect diagnostics for critical errors
                var diagnostics = await _diagnosticService.CollectDiagnosticsAsync(
                    DiagnosticLevel.Comprehensive, CancellationToken.None);

                // Export diagnostics for support
                var exportPath = await _diagnosticService.ExportDiagnosticsAsync(diagnostics, "json", CancellationToken.None);
                _logger.LogInformation("Diagnostics exported to: {ExportPath}", exportPath);

                // Show user-friendly error message
                await ShowCriticalErrorDialogAsync(exception, exportPath);
            }
            catch (Exception handlerException)
            {
                _logger.LogCritical(handlerException, "Exception handler itself failed");
                
                // Last resort: show basic error dialog
                await ShowBasicErrorDialogAsync(exception);
            }
        }

        public async Task<bool> HandleExceptionWithUserNotificationAsync(Exception exception, string operationName, string? userId = null)
        {
            _logger.LogError(exception, "Exception in operation: {OperationName} for user: {UserId}", operationName, userId);

            try
            {
                var context = new ErrorContext(operationName, userId);
                var result = await _errorHandler.HandleErrorAsync(exception, context, CancellationToken.None);

                if (result.WasRecovered)
                {
                    await ShowRecoverySuccessDialogAsync(result);
                    return true;
                }
                else
                {
                    await ShowErrorDialogAsync(result);
                    
                    // Offer troubleshooting suggestions
                    var suggestions = await _diagnosticService.GetTroubleshootingSuggestionsAsync(
                        exception, context, CancellationToken.None);
                    
                    if (suggestions.Any())
                    {
                        await ShowTroubleshootingDialogAsync(suggestions);
                    }
                    
                    return false;
                }
            }
            catch (Exception handlerException)
            {
                _logger.LogError(handlerException, "Error handler failed for operation: {OperationName}", operationName);
                await ShowBasicErrorDialogAsync(exception);
                return false;
            }
        }

        private async Task ShowCriticalErrorDialogAsync(Exception exception, string diagnosticsPath)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                // In a real implementation, this would show a proper dialog
                // For now, we'll use a simple message box concept
                var message = $"A critical error occurred in the application.\n\n" +
                             $"Error: {exception.Message}\n\n" +
                             $"Diagnostic information has been saved to:\n{diagnosticsPath}\n\n" +
                             $"Please contact support and provide this file.\n\n" +
                             $"The application will now close.";

                _logger.LogCritical("Showing critical error dialog: {Message}", message);
                
                // In a real UI, you would show an actual dialog here
                // For example: await MessageBox.ShowAsync("Critical Error", message);
                
                // For now, just log the message
                Console.WriteLine($"CRITICAL ERROR DIALOG: {message}");
            });
        }

        private async Task ShowErrorDialogAsync(ErrorHandlingResult result)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var message = result.UserMessage;
                if (!string.IsNullOrEmpty(result.RecoveryAction))
                {
                    message += $"\n\nRecovery action attempted: {result.RecoveryAction}";
                }

                _logger.LogInformation("Showing error dialog: {Message}", message);
                
                // In a real UI, you would show an actual dialog here
                Console.WriteLine($"ERROR DIALOG: {message}");
            });
        }

        private async Task ShowRecoverySuccessDialogAsync(ErrorHandlingResult result)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var message = $"The operation encountered an issue but was successfully recovered.\n\n" +
                             $"Recovery action: {result.RecoveryAction}\n\n" +
                             $"You can continue using the application normally.";

                _logger.LogInformation("Showing recovery success dialog: {Message}", message);
                
                // In a real UI, you would show an actual dialog here
                Console.WriteLine($"RECOVERY SUCCESS DIALOG: {message}");
            });
        }

        private async Task ShowTroubleshootingDialogAsync(IEnumerable<TroubleshootingSuggestion> suggestions)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var message = "Here are some troubleshooting suggestions:\n\n";
                
                foreach (var suggestion in suggestions.Take(3)) // Show top 3 suggestions
                {
                    message += $"• {suggestion.Title}\n";
                    message += $"  {suggestion.Description}\n";
                    if (suggestion.Steps.Any())
                    {
                        message += "  Steps:\n";
                        foreach (var step in suggestion.Steps.Take(3))
                        {
                            message += $"    - {step}\n";
                        }
                    }
                    message += "\n";
                }

                _logger.LogInformation("Showing troubleshooting dialog: {Message}", message);
                
                // In a real UI, you would show an actual dialog here
                Console.WriteLine($"TROUBLESHOOTING DIALOG: {message}");
            });
        }

        private async Task ShowBasicErrorDialogAsync(Exception exception)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var message = $"An unexpected error occurred:\n\n{exception.Message}\n\nPlease try again or contact support.";
                
                _logger.LogError("Showing basic error dialog: {Message}", message);
                
                // In a real UI, you would show an actual dialog here
                Console.WriteLine($"BASIC ERROR DIALOG: {message}");
            });
        }
    }
}