using System;
using AudioSensei.Bass;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Logging.Serilog;
using AudioSensei.ViewModels;
using AudioSensei.Views;
using Serilog;
using System.IO;
using AudioSensei.Configuration;

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
                .WriteTo.File(latestLogPath, outputTemplate: template)
                .WriteTo.File(@"logs\log-.log", outputTemplate: template, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 9)
                .CreateLogger();

            SerilogLogger.Initialize(logger);
            Log.Logger = logger;

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    Log.Error(ex, $"Caught an unhandled {ex.GetType().Name} exception, {(args.IsTerminating ? "" : "not ")}terminating the program");
                }
                else
                {
                    Log.Error($"Caught an unidentified unhandled exception, {(args.IsTerminating ? "" : "not ")}terminating the program");
                }
            };

            Log.Information("Opening a new instance of AudioSensei");

            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var configuration = AudioSenseiConfiguration.LoadOrCreate("config.json");
                var window = new MainWindow();
                window.DataContext = new MainWindowViewModel(new BassAudioBackend(window.PlatformImpl.Handle.Handle));
                desktop.MainWindow = window;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
