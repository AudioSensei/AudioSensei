using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Logging.Serilog;
using AudioSensei.ViewModels;
using AudioSensei.Views;
using Serilog;
using System.IO;

namespace AudioSensei
{
    public class App : Application
    {
        public override void Initialize()
        {
            const string template = "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";
            const string latestLogPath = @"logs\latest.log";

            if (File.Exists(latestLogPath))
            {
                File.Delete(latestLogPath);
            }

            var logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(latestLogPath,
                    outputTemplate: template)
                .WriteTo.File(@"logs\log-.log",
                    outputTemplate: template,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 9)
                .CreateLogger();

            SerilogLogger.Initialize(logger);
            Log.Logger = logger;

            Log.Information("Opening a new instance of AudioSensei");

            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
