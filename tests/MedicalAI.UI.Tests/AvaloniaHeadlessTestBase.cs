using System;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;

namespace MedicalAI.UI.Tests
{
    public abstract class AvaloniaHeadlessTestBase : IDisposable
    {
        private static bool _initialized;
        private static readonly object _sync = new();

        protected AvaloniaHeadlessTestBase()
        {
            EnsureInitialized();
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            lock (_sync)
            {
                if (_initialized)
                {
                    return;
                }

                var options = new AvaloniaHeadlessPlatformOptions();

                AppBuilder.Configure<App>()
                    .UseHeadless(options)
                    .LogToTrace()
                    .SetupWithoutStarting();

                _initialized = true;
            }
        }

        public virtual void Dispose()
        {
            Dispatcher.UIThread.RunJobs();
        }
    }
}
