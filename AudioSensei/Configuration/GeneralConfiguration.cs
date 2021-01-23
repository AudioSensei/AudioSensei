using Serilog.Events;

namespace AudioSensei.Configuration
{
    public sealed class GeneralConfiguration
    {
        public LogEventLevel LoggerMinimumLevel { get; set; } = LogEventLevel.Information;
        public bool EnableMessageBoxSink { get; set; } = true;
        public LogEventLevel MessageBoxSinkMinimumLevel { get; set; } = LogEventLevel.Warning;
        public string LogTemplate { get; set; } = "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";
        public string LogTimeFormat { get; set; } = "yyyyMMddHHmm";
    }
}
