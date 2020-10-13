using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace AudioSensei
{
    public class MessageBoxSink : ILogEventSink
    {
#if WINDOWS
        private const string User32 = "user32";

        [DllImport(User32, EntryPoint = "MessageBoxW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int MessageBoxWINDOWS(IntPtr hwnd, string message, string title, uint flags);
#else
        private const string SdlDll = "SDL2";

        [DllImport(SdlDll, EntryPoint = "SDL_ShowSimpleMessageBox")]
        private static extern int MessageBoxSdl(uint flags, [MarshalAs(UnmanagedType.LPUTF8Str)] string title, [MarshalAs(UnmanagedType.LPUTF8Str)] string message, IntPtr parent);

        [DllImport(SdlDll, EntryPoint = "SDL_GetError")]
        [return: MarshalAs(UnmanagedType.LPUTF8Str)]
        private static extern string SdlGetError();

        [DllImport(SdlDll, EntryPoint = "SDL_GetVersion")]
        private static extern void SdlGetVersion(out SdlVersion version);

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct SdlVersion
        {
            public readonly byte Major;
            public readonly byte Minor;
            public readonly byte Patch;

            public override string ToString() => $"{Major}.{Minor}.{Patch}";
        }
#endif

        private readonly IFormatProvider _formatProvider;
        private readonly LogEventLevel _restrictedToMinimumLevel;

        public MessageBoxSink(IFormatProvider formatProvider, LogEventLevel restrictedToMinimumLevel)
        {
            _formatProvider = formatProvider;
            _restrictedToMinimumLevel = restrictedToMinimumLevel;

#if !WINDOWS
            SdlGetVersion(out var version);
            SelfLog.WriteLine($"Loaded SDL version {version}");
#endif
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent.Level < _restrictedToMinimumLevel)
            {
                return;
            }
            var message = logEvent.RenderMessage(_formatProvider);
            var title = $"[{logEvent.Level}] AudioSensei";

            void ShowDialog()
            {
#if WINDOWS
                uint flags = logEvent.Level switch
                {
                    LogEventLevel.Verbose => 0x00000000 | 0x00000040,
                    LogEventLevel.Debug => 0x00000000 | 0x00000040,
                    LogEventLevel.Information => 0x00000000 | 0x00000040,
                    LogEventLevel.Warning => 0x00000000 | 0x00000030,
                    LogEventLevel.Error => 0x00000000 | 0x00000010,
                    LogEventLevel.Fatal => 0x00000000 | 0x00000010,
                    _ => throw new ArgumentOutOfRangeException(nameof(logEvent.Level), logEvent.Level, "Invalid LogEventLevel")
                };
                if (MessageBoxWINDOWS(IntPtr.Zero, message, title, flags) == 0)
                    throw new Win32Exception();
#else
                uint sdlflags = logEvent.Level switch
                {
                    LogEventLevel.Verbose => 0x00000040,
                    LogEventLevel.Debug => 0x00000040,
                    LogEventLevel.Information => 0x00000040,
                    LogEventLevel.Warning => 0x00000020,
                    LogEventLevel.Error => 0x00000010,
                    LogEventLevel.Fatal => 0x00000010,
                    _ => throw new ArgumentOutOfRangeException(nameof(logEvent.Level), logEvent.Level, "Invalid LogEventLevel")
                };
                if (MessageBoxSdl(sdlflags, title, message, IntPtr.Zero) != 0)
                    throw new Exception(SdlGetError());
#endif
            }

            new Thread(ShowDialog) { IsBackground = false, Priority = ThreadPriority.BelowNormal }.Start();
        }
    }

    public static class MessageBoxSinkExtensions
    {
        public static LoggerConfiguration MessageBox(
            this LoggerSinkConfiguration loggerConfiguration,
            IFormatProvider formatProvider = null, LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose)
        {
            return loggerConfiguration.Sink(new MessageBoxSink(formatProvider, restrictedToMinimumLevel));
        }
    }
}
