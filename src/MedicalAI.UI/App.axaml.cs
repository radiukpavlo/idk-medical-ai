using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MedicalAI.Infrastructure.DI;
using MedicalAI.UI.Services;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace MedicalAI.UI
{
    public partial class App : Avalonia.Application
    {
        public static ServiceProvider Services { get; private set; } = default!;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var sc = new ServiceCollection();
            
            // Add logging
            sc.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            
            // Add infrastructure services
            sc.AddInfrastructure();
            
            // Add UI services
            sc.AddScoped<IUIPerformanceService, UIPerformanceService>();
            sc.AddSingleton<IGlobalExceptionHandler, GlobalExceptionHandler>();
            
            Services = sc.BuildServiceProvider();

            // Start memory monitoring
            var memoryManager = Services.GetRequiredService<MedicalAI.Core.Performance.IMemoryManager>();
            memoryManager.StartMemoryPressureMonitoring(CancellationToken.None);

            // Set up global exception handling
            SetupGlobalExceptionHandling();

            Thread.CurrentThread.CurrentUICulture = new CultureInfo("uk-UA");

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }
            base.OnFrameworkInitializationCompleted();
        }

        private void SetupGlobalExceptionHandling()
        {
            // Handle unhandled exceptions in the UI thread
            AppDomain.CurrentDomain.UnhandledException += async (sender, e) =>
            {
                var exception = e.ExceptionObject as Exception ?? new Exception("Unknown error occurred");
                var handler = Services.GetRequiredService<IGlobalExceptionHandler>();
                await handler.HandleUnhandledExceptionAsync(exception, "AppDomain.UnhandledException");
            };

            // Handle unhandled exceptions in tasks
            TaskScheduler.UnobservedTaskException += async (sender, e) =>
            {
                var handler = Services.GetRequiredService<IGlobalExceptionHandler>();
                await handler.HandleUnhandledExceptionAsync(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved(); // Prevent the process from terminating
            };
        }
    }
}
