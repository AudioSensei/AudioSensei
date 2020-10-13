using System;
using System.IO;
using AudioSensei.Bass;
using AudioSensei.Configuration;
using AudioSensei.ViewModels;
using AudioSensei.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.File.GZip;

namespace AudioSensei
{
    public class App : Application
    {
        public static Window MainWindow { get; private set; }
        public static string ApplicationDataPath { get; }

        static App()
        {
            ApplicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioSensei");

            if (!Directory.Exists(ApplicationDataPath))
            {
                Directory.CreateDirectory(ApplicationDataPath);
            }
        }

        public override void Initialize()
        {
            const string template = "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";
            string directory = Path.Combine(ApplicationDataPath, "logs");
            string latestLogPath = Path.Combine(directory, "latest.log");
            string rollingLogPath = Path.Combine(directory, $"log-{DateTimeOffset.Now:yyyyMMddHHmm}.log.gz");

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(latestLogPath))
            {
                File.Delete(latestLogPath);
            }

            var logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(latestLogPath, outputTemplate: template)
                .WriteTo.File(rollingLogPath, outputTemplate: template, buffered: true, hooks: new GZipHooks())
                .WriteTo.MessageBox(restrictedToMinimumLevel: LogEventLevel.Warning)
                .CreateLogger();

            Avalonia.Logging.Logger.Sink = new AvaloniaSerilogSink(logger);
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
                var configuration = AudioSenseiConfiguration.LoadOrCreate(Path.Combine(ApplicationDataPath, "config.json"));
                MainWindow = new MainWindow();
                MainWindow.DataContext = new MainWindowViewModel(new BassAudioBackend(MainWindow.PlatformImpl.Handle.Handle));
                desktop.MainWindow = MainWindow;
                desktop.Exit += (sender, args) => Log.CloseAndFlush();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
