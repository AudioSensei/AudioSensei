using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Serilog;

namespace AudioSensei.Bass
{
    internal static class BassNative
    {
#if X64
        private const string Arch = "X64";
#elif X86
        private const string Arch = "X86";
#elif ARM
        private const string Arch = "ARM";
#elif ARM64
        private const string Arch = "ARM64";
#endif

        private const string Bass = "bass";

#if WINDOWS
        private const UnmanagedType StringMarshal = UnmanagedType.LPWStr;
        private const StreamFlags UnicodeFlag = StreamFlags.Unicode;
        private const uint PluginUnicodeFlag = 0x80000000;
#else
        private const UnmanagedType StringMarshal = UnmanagedType.LPUTF8Str;
        private const StreamFlags UnicodeFlag = StreamFlags.None;
        private const uint PluginUnicodeFlag = 0;
#endif

        private static readonly object LoadLock = new object();
        private static StreamFlags _floatFlag = StreamFlags.None;

#if WINDOWS
        static BassNative()
        {
            BASS_SetConfig(BassConfig.Unicode, 1);
        }
#endif

        public static unsafe void Initialize(int device = -1, uint frequency = 44000)
        {
            lock (LoadLock)
            {
                Log.Information($"Loading Bass version {BASS_GetVersion()}");

#if WINDOWS
                const string filter = "*." + Arch + ".dll";
#elif LINUX
                const string filter = "*." + Arch + ".so";
#elif OSX
                const string filter = "*." + Arch + ".dylib";
#endif

                foreach (var file in Directory.EnumerateFiles("BassPlugins", filter, SearchOption.AllDirectories))
                {
                    var path = Path.GetFullPath(file);
                    var handle = BASS_PluginLoad(path, PluginUnicodeFlag);
                    if (handle == 0)
                        throw new BassException($"Loading {path} as plugin failed");
                    var infoPtr = BASS_PluginGetInfo(handle);
                    if (infoPtr == null)
                        throw new BassException($"Getting plugin info for {path} failed");
                    var info = *infoPtr;
                    Log.Information($"Loaded Bass plugin {Path.GetFileNameWithoutExtension(path)} version {info.version}. Supported formats: {string.Join(", ", ListSupportedFormats(info))}");
                }

                // ReSharper disable once RedundantAssignment
                var window = IntPtr.Zero;
#if WINDOWS
                window = GetActiveWindow();
#endif

                if (!BASS_Init(device, frequency, 0, window, IntPtr.Zero))
                {
                    throw new BassException("Init failed");
                }

                if (BASS_GetConfig(BassConfig.Float) == 0)
                    _floatFlag = StreamFlags.None;
                else
                {
                    BassHandle floatable = BASS_StreamCreate(44100, 1, StreamFlags.SampleFloat, IntPtr.Zero, IntPtr.Zero); // try creating a floating-point stream
                    if (floatable != BassHandle.Null)
                    {
                        BASS_StreamFree(floatable); // floating-point channels are supported (free the test stream)
                        _floatFlag = StreamFlags.SampleFloat;
                    }
                    else
                    {
                        _floatFlag = StreamFlags.None;
                    }
                }

                if (_floatFlag.HasFlag(StreamFlags.SampleFloat))
                {
                    Log.Information("Enabling floating point data output");
                }


                Log.Information($"Setting useragent to {WebHelper.UserAgent}");
                BASS_SetConfigPtr(BassConfig.NetAgent, WebHelper.UserAgent);
                
                Log.Information("Bass initialization complete");
            }
        }

        private static IEnumerable<string> ListSupportedFormats(BassPluginInfo info)
        {
            for (int i = 0; i < info.formatc; i++)
            {
                var f = info.GetFormatAt(i);
                yield return $"Format: {Marshal.PtrToStringUTF8(f.name)} - extensions: {Marshal.PtrToStringUTF8(f.exts)}";
            }
        }

#if WINDOWS
        [DllImport("user32")]
        private static extern IntPtr GetActiveWindow();
#endif

        public static void Free()
        {
            lock (LoadLock)
            {
                BASS_PluginFree(0);
                if (!BASS_Free())
                {
                    throw new BassException("Free failed");
                }
            }
        }

        public static BassHandle CreateStreamFromFile(string filePath)
        {
            var handle = BASS_StreamCreateFile(false, filePath, 0, 0, _floatFlag | StreamFlags.AsyncFile | StreamFlags.StreamPrescan | StreamFlags.StreamAutoFree | UnicodeFlag);

            if (handle == BassHandle.Null)
            {
                throw new BassException("StreamCreateFile failed");
            }

            return handle;
        }

        public static BassHandle CreateStreamFromUrl(string url)
        {
            var handle = BASS_StreamCreateURL(url, 0, _floatFlag | StreamFlags.StreamAutoFree | StreamFlags.StreamBlock | StreamFlags.StreamRestrate | UnicodeFlag, null, IntPtr.Zero);

            if (handle == BassHandle.Null)
            {
                throw new BassException("StreamCreateUrl failed");
            }

            return handle;
        }

        public static void FreeStream(this BassHandle handle)
        {
            if (!handle.BASS_StreamFree())
            {
                throw new BassException("StreamFree failed");
            }
        }

        public static void PlayChannel(this BassHandle handle, bool restart = false)
        {
            if (!handle.BASS_ChannelPlay(restart))
            {
                throw new BassException("ChannelPlay failed");
            }
        }

        public static void PauseChannel(this BassHandle handle)
        {
            if (!handle.BASS_ChannelPause())
            {
                throw new BassException("ChannelPause failed");
            }
        }

        public static void StopChannel(this BassHandle handle)
        {
            if (!handle.BASS_ChannelStop())
            {
                throw new BassException("ChannelStop failed");
            }
        }

        public static float GetChannelAttribute(this BassHandle handle, ChannelAttribute attribute)
        {
            if (!handle.BASS_ChannelGetAttribute(attribute, out var value))
            {
                throw new BassException("ChannelGetAttribute failed");
            }

            return value;
        }

        public static void SetChannelAttribute(this BassHandle handle, ChannelAttribute attribute, float value)
        {
            if (!handle.BASS_ChannelSetAttribute(attribute, value))
            {
                throw new BassException("ChannelSetAttribute failed");
            }
        }

        public static ulong GetChannelLength(this BassHandle handle, LengthMode mode)
        {
            return handle.BASS_ChannelGetLength(mode);
        }

        public static ulong GetChannelPosition(this BassHandle handle, LengthMode mode)
        {
            return handle.BASS_ChannelGetPosition(mode);
        }

        public static ChannelStatus GetChannelStatus(this BassHandle handle)
        {
            return handle.BASS_ChannelIsActive();
        }

        public static double ConvertBytesToSeconds(this BassHandle handle, ulong position)
        {
            return handle.BASS_ChannelBytes2Seconds(position);
        }

        public static uint GetLastErrorCode()
        {
            return BASS_ErrorGetCode();
        }

        [DllImport(Bass)]
        private static extern uint BASS_PluginLoad([MarshalAs(StringMarshal)] string file, uint flags);

        [DllImport(Bass)]
        private static extern bool BASS_PluginFree(uint handle);

        [DllImport(Bass)]
        private static extern unsafe BassPluginInfo* BASS_PluginGetInfo(uint handle);

        [DllImport(Bass)]
        private static extern bool BASS_Init(int device, uint frequency, uint flags, IntPtr window, IntPtr clsid);

        [DllImport(Bass)]
        private static extern bool BASS_Free();

        [DllImport(Bass)]
        private static extern bool BASS_StreamFree(this BassHandle handle);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelPlay(this BassHandle handle, bool restart = false);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelPause(this BassHandle handle);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelStop(this BassHandle handle);

        [DllImport(Bass)]
        private static extern ChannelStatus BASS_ChannelIsActive(this BassHandle handle);

        [DllImport(Bass)]
        private static extern ulong BASS_ChannelGetLength(this BassHandle handle, LengthMode mode);

        [DllImport(Bass)]
        private static extern ulong BASS_ChannelGetPosition(this BassHandle handle, LengthMode mode);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelGetAttribute(this BassHandle handle, ChannelAttribute attribute, out float value);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelSetAttribute(this BassHandle handle, ChannelAttribute attribute, float value);

        [DllImport(Bass)]
        private static extern double BASS_ChannelBytes2Seconds(this BassHandle handle, ulong position);

        [DllImport(Bass)]
        private static extern uint BASS_ErrorGetCode();

        [DllImport(Bass)]
        private static extern BassVersion BASS_GetVersion();

        [DllImport(Bass)]
        private static extern uint BASS_GetConfig(BassConfig option);

        [DllImport(Bass)]
        private static extern IntPtr BASS_GetConfigPtr(BassConfig option);

        [DllImport(Bass)]
        private static extern bool BASS_SetConfig(BassConfig option, uint value);

        [DllImport(Bass)]
        private static extern bool BASS_SetConfigPtr(BassConfig option, IntPtr value);

        [DllImport(Bass)]
        private static extern bool BASS_SetConfigPtr(BassConfig option, [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

        [DllImport(Bass)]
        private static extern BassHandle BASS_StreamCreateFile(bool memory, [MarshalAs(StringMarshal)] string file, ulong offset, ulong length, StreamFlags flags);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void DownloadProc(IntPtr buffer, uint length, IntPtr user);

        [DllImport(Bass)]
        private static extern BassHandle BASS_StreamCreateURL([MarshalAs(StringMarshal)] string url, uint offset, StreamFlags flags, DownloadProc proc, IntPtr user);

        [DllImport(Bass)]
        private static extern BassHandle BASS_StreamCreate(uint freq, uint chans, StreamFlags flags, IntPtr proc, IntPtr user);
    }
}
