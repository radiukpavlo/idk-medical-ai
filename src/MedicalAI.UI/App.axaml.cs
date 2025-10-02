using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using MedicalAI.Infrastructure.DI;
using System.Globalization;
using System.Threading;

namespace MedicalAI.UI
{
    public partial class App : Avalonia.Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var services = new ServiceCollection();
            services.AddInfrastructure();
            services.AddTransient<MainWindow>(); // Register MainWindow for DI

            var serviceProvider = services.BuildServiceProvider();

            Thread.CurrentThread.CurrentUICulture = new CultureInfo("uk-UA");

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = serviceProvider.GetRequiredService<MainWindow>();
            }
            base.OnFrameworkInitializationCompleted();
        }
    }
}