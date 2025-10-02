using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using MedicalAI.Infrastructure.DI;
using System.Globalization;
using System.Threading;

namespace MedicalAI.UI
{
    public partial class App : Application
    {
        public static ServiceProvider Services { get; private set; } = default!;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var sc = new ServiceCollection();
            sc.AddInfrastructure();
            Services = sc.BuildServiceProvider();

            Thread.CurrentThread.CurrentUICulture = new CultureInfo("uk-UA");

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }
            base.OnFrameworkInitializationCompleted();
        }
    }
}
