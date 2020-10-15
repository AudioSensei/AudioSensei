using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Serilog;

#if NO_RID
#warning No RuntimeIdentifier set, building for current platform!
#endif

#if INVALID_OS
#error Unsupported OS!
#endif

#if INVALID_ARCH
#error Unsupported architecture!
#endif

#if NO_LIBRARY
#error Target doesn't support a library!
#endif

namespace AudioSensei
{
    class Program
    {
        public static event Action Exit;

        private static readonly object ExitLock = new object();
        private static bool _exited;

        public static void TriggerExit()
        {
            lock (ExitLock)
            {
                if (_exited)
                {
                    return;
                }

                Log.Information("AudioSensei is exitting");
                _exited = true;
                try
                {
                    Exit?.Invoke();
                }
                catch
                {
                    // ignore
                }
            }
        }

        public static void RegisterExitOnSameThread(Action action)
        {
            int thread = Environment.CurrentManagedThreadId;
            Exit += () =>
            {
                if (thread == Environment.CurrentManagedThreadId)
                {
                    action();
                }
            };
        }

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose);
            }
            finally
            {
                TriggerExit();
            }
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToDebug()
                .UseReactiveUI();
    }
}
