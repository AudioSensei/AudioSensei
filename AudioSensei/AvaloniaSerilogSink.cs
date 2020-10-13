using System;
using Avalonia.Logging;
using Serilog;

namespace AudioSensei
{
    public class AvaloniaSerilogSink : ILogSink
    {
        private readonly ILogger _logger;

        public AvaloniaSerilogSink(ILogger logger)
        {
            _logger = logger;
        }

        public bool IsEnabled(LogEventLevel level, string area)
        {
            return _logger.IsEnabled(ConvertLevel(level));
        }

        public void Log(LogEventLevel level, string area, object source, string messageTemplate)
        {
            switch (level)
            {
                case LogEventLevel.Verbose:
                    _logger.Verbose($"[{area} - {source}] {messageTemplate}");
                    break;
                case LogEventLevel.Debug:
                    _logger.Debug($"[{area} - {source}] {messageTemplate}");
                    break;
                case LogEventLevel.Information:
                    _logger.Information($"[{area} - {source}] {messageTemplate}");
                    break;
                case LogEventLevel.Warning:
                    _logger.Warning($"[{area} - {source}] {messageTemplate}");
                    break;
                case LogEventLevel.Error:
                    _logger.Error($"[{area} - {source}] {messageTemplate}");
                    break;
                case LogEventLevel.Fatal:
                    _logger.Fatal($"[{area} - {source}] {messageTemplate}");
                    break;
                default:
                    _logger.Information($"[{area} - {source}] {messageTemplate}");
                    break;
            }
        }

        public void Log<T0>(LogEventLevel level, string area, object source, string messageTemplate, T0 propertyValue0)
        {
            switch (level)
            {
                case LogEventLevel.Verbose:
                    _logger.Verbose($"[{area} - {source}] {messageTemplate}", propertyValue0);
                    break;
                case LogEventLevel.Debug:
                    _logger.Debug($"[{area} - {source}] {messageTemplate}", propertyValue0);
                    break;
                case LogEventLevel.Information:
                    _logger.Information($"[{area} - {source}] {messageTemplate}", propertyValue0);
                    break;
                case LogEventLevel.Warning:
                    _logger.Warning($"[{area} - {source}] {messageTemplate}", propertyValue0);
                    break;
                case LogEventLevel.Error:
                    _logger.Error($"[{area} - {source}] {messageTemplate}", propertyValue0);
                    break;
                case LogEventLevel.Fatal:
                    _logger.Fatal($"[{area} - {source}] {messageTemplate}", propertyValue0);
                    break;
                default:
                    _logger.Information($"[{area} - {source}] {messageTemplate}", propertyValue0);
                    break;
            }
        }

        public void Log<T0, T1>(LogEventLevel level, string area, object source, string messageTemplate, T0 propertyValue0, T1 propertyValue1)
        {
            switch (level)
            {
                case LogEventLevel.Verbose:
                    _logger.Verbose($"[{area} - {source}] {messageTemplate}", propertyValue0, propertyValue1);
                    break;
                case LogEventLevel.Debug:
                    _logger.Debug($"[{area} - {source}] {messageTemplate}", propertyValue0, propertyValue1);
                    break;
                case LogEventLevel.Information:
                    _logger.Information($"[{area} - {source}] {messageTemplate}", propertyValue0, propertyValue1);
                    break;
                case LogEventLevel.Warning:
                    _logger.Warning($"[{area} - {source}] {messageTemplate}", propertyValue0, propertyValue1);
                    break;
                case LogEventLevel.Error:
                    _logger.Error($"[{area} - {source}] {messageTemplate}", propertyValue0, propertyValue1);
                    break;
                case LogEventLevel.Fatal:
                    _logger.Fatal($"[{area} - {source}] {messageTemplate}", propertyValue0, propertyValue1);
                    break;
                default:
                    _logger.Information($"[{area} - {source}] {messageTemplate}", propertyValue0, propertyValue1);
                    break;
            }
        }

        public void Log<T0, T1, T2>(LogEventLevel level, string area, object source, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
        {
            switch (level)
            {
                case LogEventLevel.Verbose:
                    _logger.Verbose($"[{area} - {source}] {messageTemplate}", propertyValue0, propertyValue1, propertyValue2);
                    break;
                case LogEventLevel.Debug:
                    _logger.Debug($"[{area} - {source}] {messageTemplate}", propertyValue0, propertyValue1, propertyValue2);
                    break;
                case LogEventLevel.Information:
                    _logger.Information($"[{area} - {source}] {messageTemplate}", propertyValue0, propertyValue1, propertyValue2);
                    break;
                case LogEventLevel.Warning:
                    _logger.Warning($"[{area} - {source}] {messageTemplate}", propertyValue0, propertyValue1, propertyValue2);
                    break;
                case LogEventLevel.Error:
                    _logger.Error($"[{area} - {source}] {messageTemplate}", propertyValue0, propertyValue1, propertyValue2);
                    break;
                case LogEventLevel.Fatal:
                    _logger.Fatal($"[{area} - {source}] {messageTemplate}", propertyValue0, propertyValue1, propertyValue2);
                    break;
                default:
                    _logger.Information($"[{area} - {source}] {messageTemplate}", propertyValue0, propertyValue1, propertyValue2);
                    break;
            }
        }

        public void Log(LogEventLevel level, string area, object source, string messageTemplate, params object[] propertyValues)
        {
            switch (level)
            {
                case LogEventLevel.Verbose:
                    _logger.Verbose($"[{area} - {source}] {messageTemplate}", propertyValues);
                    break;
                case LogEventLevel.Debug:
                    _logger.Debug($"[{area} - {source}] {messageTemplate}", propertyValues);
                    break;
                case LogEventLevel.Information:
                    _logger.Information($"[{area} - {source}] {messageTemplate}", propertyValues);
                    break;
                case LogEventLevel.Warning:
                    _logger.Warning($"[{area} - {source}] {messageTemplate}", propertyValues);
                    break;
                case LogEventLevel.Error:
                    _logger.Error($"[{area} - {source}] {messageTemplate}", propertyValues);
                    break;
                case LogEventLevel.Fatal:
                    _logger.Fatal($"[{area} - {source}] {messageTemplate}", propertyValues);
                    break;
                default:
                    _logger.Information($"[{area} - {source}] {messageTemplate}", propertyValues);
                    break;
            }
        }

        private static Serilog.Events.LogEventLevel ConvertLevel(LogEventLevel level)
        {
            return level switch
            {
                LogEventLevel.Verbose => Serilog.Events.LogEventLevel.Verbose,
                LogEventLevel.Debug => Serilog.Events.LogEventLevel.Debug,
                LogEventLevel.Information => Serilog.Events.LogEventLevel.Information,
                LogEventLevel.Warning => Serilog.Events.LogEventLevel.Warning,
                LogEventLevel.Error => Serilog.Events.LogEventLevel.Error,
                LogEventLevel.Fatal => Serilog.Events.LogEventLevel.Fatal,
                _ => Serilog.Events.LogEventLevel.Information
            };
        }
    }
}
