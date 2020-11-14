using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using AudioSensei.Bass.Native.Handles;
using AudioSensei.Configuration;
using Serilog;

namespace AudioSensei.Bass.Native
{
    internal class BassNative : IDisposable
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
        public static BassNative Singleton { get; private set; }
        private static bool _invalidState;

        public readonly ConcurrentDictionary<PluginHandle, (BassPluginManifest manifest, BassPluginInfo info)> Plugins;
        
        private readonly BassConfiguration _bassConfiguration;
        
        private readonly BassInitFlags _flags;
        private readonly IntPtr _windowHandle;

        private readonly StreamFlags _floatFlag;
        private readonly StreamFlags _restrateFlag;

#if WINDOWS
        static BassNative()
        {
            BASS_SetConfig(BassConfig.Unicode, 1);
        }
#endif

        public unsafe BassNative(BassConfiguration bassConfiguration, IntPtr windowHandle = default)
        {
            lock (LoadLock)
            {
                try
                {
                    if (Singleton != null)
                    {
                        throw new InvalidOperationException();
                    }

                    if (_invalidState)
                    {
                        throw new InvalidOperationException();
                    }

                    Log.Information($"Loading Bass version {BASS_GetVersion()}");

                    const string filter = "*.manifest";

                    Plugins = new ConcurrentDictionary<PluginHandle, (BassPluginManifest, BassPluginInfo)>();
                    foreach (var file in Directory.EnumerateFiles(
                        Directory.Exists("BassPlugins")
                            ? "BassPlugins"
                            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BassPlugins"), filter,
                        SearchOption.AllDirectories))
                    {
                        var manifest = BassPluginManifest.Load(file);

#if WINDOWS
                        const string os = "windows";
#elif LINUX
                        const string os = "linux";
#elif OSX
                        const string os = "osx";
#endif

                        var osDict = new Dictionary<string, Dictionary<string, string>>(manifest.Library, StringComparer.OrdinalIgnoreCase);
                        if (!osDict.TryGetValue(os, out var aDictTemp))
                        {
                            Log.Information($"Skipping loading {manifest.Name} due to unsupported OS");
                            continue;
                        }

                        var archDict = new Dictionary<string, string>(aDictTemp, StringComparer.OrdinalIgnoreCase);
                        if (!archDict.TryGetValue(Arch, out var library))
                        {
                            Log.Information($"Skipping loading {manifest.Name} due to unsupported architecture");
                            continue;
                        }

                        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file) ?? string.Empty, library));

                        var handle = BASS_PluginLoad(path, PluginUnicodeFlag);
                        if (handle == PluginHandle.Null)
                            throw new BassException($"Loading {path} as plugin failed");
                        var infoPtr = BASS_PluginGetInfo(handle);
                        if (infoPtr == null)
                            throw new BassException($"Getting plugin info for {path} failed");
                        var info = *infoPtr;
                        Log.Information($"Loaded Bass plugin {manifest.Name} from {path} version {info.version}. Supported formats: {string.Join(", ", info.ListSupportedFormats())}");
                        Plugins[handle] = (manifest, info);
                    }

#if WINDOWS
                    if (windowHandle == IntPtr.Zero)
                    {
                        windowHandle = GetActiveWindow();
                    }
#endif

                    _bassConfiguration = bassConfiguration;

                    _flags = 0;
                    _windowHandle = windowHandle;
                    if (!BASS_Init(bassConfiguration.Device, bassConfiguration.Frequency, _flags, _windowHandle, IntPtr.Zero))
                    {
                        throw new BassException("Init failed");
                    }

                    if (BASS_GetConfig(BassConfig.Float) == 0)
                        _floatFlag = StreamFlags.None;
                    else
                    {
                        var floatable = BASS_StreamCreate(44100, 1, StreamFlags.SampleFloat, IntPtr.Zero, IntPtr.Zero); // try creating a floating-point stream
                        if (floatable != StreamHandle.Null)
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

                    _restrateFlag = bassConfiguration.Restrate ? StreamFlags.StreamRestrate : StreamFlags.None;

                    if (_restrateFlag.HasFlag(StreamFlags.StreamRestrate))
                    {
                        Log.Information("Enabling stream restrate");
                    }

                    Log.Information($"Setting useragent to {WebHelper.UserAgent}");
                    BASS_SetConfigPtr(BassConfig.NetAgent, WebHelper.UserAgent);

                    Log.Information("Bass initialization complete");

                    Singleton = this;
                }
                finally
                {
                    if (Singleton == null)
                    {
                        _invalidState = true;
                    }
                }
            }
        }

#if WINDOWS
        [DllImport("user32")]
        private static extern IntPtr GetActiveWindow();
#endif

        private static void Free()
        {
            lock (LoadLock)
            {
                BASS_PluginFree(PluginHandle.Null);
                if (!BASS_Free())
                {
                    throw new BassException("Free failed");
                }

                Singleton = null;
            }
        }

        public void Dispose()
        {
            Free();
            GC.SuppressFinalize(this);
        }

        ~BassNative()
        {
            Free();
        }

        public void Restart()
        {
            // ReSharper disable InconsistentlySynchronizedField
            if (!((BASS_GetInfo(out var info) && info.initflags.HasFlag(BassInitFlags.DeviceDSound) &&
                   BASS_Init(_bassConfiguration.Device, _bassConfiguration.Frequency, _flags, _windowHandle, IntPtr.Zero)) || BASS_Start()))
            {
                throw new BassException("Restart failed");
            }
            // ReSharper restore InconsistentlySynchronizedField
        }

        public StreamHandle CreateStreamFromFile(string filePath)
        {
            var handle = BASS_StreamCreateFile(false, filePath, 0, 0, _floatFlag | StreamFlags.AsyncFile | StreamFlags.StreamPrescan | StreamFlags.StreamAutoFree | UnicodeFlag);

            if (handle == StreamHandle.Null)
            {
                throw new BassException("StreamCreateFile failed");
            }

            return handle;
        }

        public StreamHandle CreateStreamFromUrl(string url, string[] webHeaders = null)
        {
            if (webHeaders != null && webHeaders.Length > 0)
            {
                url += "\r\n" + string.Join("\r\n", webHeaders) + "\r\n";
            }

            var handle = BASS_StreamCreateURL(url, 0, _floatFlag | _restrateFlag | StreamFlags.StreamAutoFree | UnicodeFlag, null, IntPtr.Zero);

            if (handle == StreamHandle.Null)
            {
                throw new BassException("StreamCreateUrl failed");
            }

            return handle;
        }

        public void FreeStream(StreamHandle handle)
        {
            if (!BASS_StreamFree(handle))
            {
                throw new BassException("StreamFree failed");
            }
        }

        public void PlayChannel(ChannelHandle handle, bool restart = false)
        {
            if (!BASS_ChannelPlay(handle, restart))
            {
                throw new BassException("ChannelPlay failed");
            }
        }

        public void PlayChannel(MusicHandle handle, bool restart = false)
        {
            if (!BASS_ChannelPlay(handle, restart))
            {
                throw new BassException("ChannelPlay failed");
            }
        }

        public void PlayChannel(StreamHandle handle, bool restart = false)
        {
            if (!BASS_ChannelPlay(handle, restart))
            {
                throw new BassException("ChannelPlay failed");
            }
        }

        public void PlayChannel(RecordHandle handle, bool restart = false)
        {
            if (!BASS_ChannelPlay(handle, restart))
            {
                throw new BassException("ChannelPlay failed");
            }
        }

        public void PauseChannel(ChannelHandle handle)
        {
            if (!BASS_ChannelPause(handle))
            {
                throw new BassException("ChannelPause failed");
            }
        }

        public void PauseChannel(MusicHandle handle)
        {
            if (!BASS_ChannelPause(handle))
            {
                throw new BassException("ChannelPause failed");
            }
        }

        public void PauseChannel(StreamHandle handle)
        {
            if (!BASS_ChannelPause(handle))
            {
                throw new BassException("ChannelPause failed");
            }
        }

        public void PauseChannel(RecordHandle handle)
        {
            if (!BASS_ChannelPause(handle))
            {
                throw new BassException("ChannelPause failed");
            }
        }

        public void StopChannel(ChannelHandle handle)
        {
            if (!BASS_ChannelStop(handle))
            {
                throw new BassException("ChannelStop failed");
            }
        }

        public void StopChannel(MusicHandle handle)
        {
            if (!BASS_ChannelStop(handle))
            {
                throw new BassException("ChannelStop failed");
            }
        }

        public void StopChannel(StreamHandle handle)
        {
            if (!BASS_ChannelStop(handle))
            {
                throw new BassException("ChannelStop failed");
            }
        }

        public void StopChannel(RecordHandle handle)
        {
            if (!BASS_ChannelStop(handle))
            {
                throw new BassException("ChannelStop failed");
            }
        }

        public float GetChannelAttribute(ChannelHandle handle, ChannelAttribute attribute)
        {
            if (!BASS_ChannelGetAttribute(handle, attribute, out var value))
            {
                throw new BassException("ChannelGetAttribute failed");
            }

            return value;
        }

        public float GetChannelAttribute(MusicHandle handle, ChannelAttribute attribute)
        {
            if (!BASS_ChannelGetAttribute(handle, attribute, out var value))
            {
                throw new BassException("ChannelGetAttribute failed");
            }

            return value;
        }

        public float GetChannelAttribute(StreamHandle handle, ChannelAttribute attribute)
        {
            if (!BASS_ChannelGetAttribute(handle, attribute, out var value))
            {
                throw new BassException("ChannelGetAttribute failed");
            }

            return value;
        }

        public float GetChannelAttribute(RecordHandle handle, ChannelAttribute attribute)
        {
            if (!BASS_ChannelGetAttribute(handle, attribute, out var value))
            {
                throw new BassException("ChannelGetAttribute failed");
            }

            return value;
        }

        public void SetChannelAttribute(ChannelHandle handle, ChannelAttribute attribute, float value)
        {
            if (!BASS_ChannelSetAttribute(handle, attribute, value))
            {
                throw new BassException("ChannelSetAttribute failed");
            }
        }

        public void SetChannelAttribute(MusicHandle handle, ChannelAttribute attribute, float value)
        {
            if (!BASS_ChannelSetAttribute(handle, attribute, value))
            {
                throw new BassException("ChannelSetAttribute failed");
            }
        }

        public void SetChannelAttribute(StreamHandle handle, ChannelAttribute attribute, float value)
        {
            if (!BASS_ChannelSetAttribute(handle, attribute, value))
            {
                throw new BassException("ChannelSetAttribute failed");
            }
        }

        public void SetChannelAttribute(RecordHandle handle, ChannelAttribute attribute, float value)
        {
            if (!BASS_ChannelSetAttribute(handle, attribute, value))
            {
                throw new BassException("ChannelSetAttribute failed");
            }
        }

        public BassChannelInfo GetChannelInfo(ChannelHandle handle)
        {
            if (!BASS_ChannelGetInfo(handle, out var info))
            {
                throw new BassException("ChannelSetAttribute failed");
            }

            return info;
        }

        public BassChannelInfo GetChannelInfo(MusicHandle handle)
        {
            if (!BASS_ChannelGetInfo(handle, out var info))
            {
                throw new BassException("ChannelSetAttribute failed");
            }

            return info;
        }

        public BassChannelInfo GetChannelInfo(StreamHandle handle)
        {
            if (!BASS_ChannelGetInfo(handle, out var info))
            {
                throw new BassException("ChannelSetAttribute failed");
            }

            return info;
        }

        public BassChannelInfo GetChannelInfo(RecordHandle handle)
        {
            if (!BASS_ChannelGetInfo(handle, out var info))
            {
                throw new BassException("ChannelSetAttribute failed");
            }

            return info;
        }

        public ulong GetChannelLength(ChannelHandle handle, LengthMode mode)
        {
            return BASS_ChannelGetLength(handle, mode);
        }

        public ulong GetChannelLength(MusicHandle handle, LengthMode mode)
        {
            return BASS_ChannelGetLength(handle, mode);
        }

        public ulong GetChannelLength(StreamHandle handle, LengthMode mode)
        {
            return BASS_ChannelGetLength(handle, mode);
        }

        public ulong GetChannelLength(SampleHandle handle, LengthMode mode)
        {
            return BASS_ChannelGetLength(handle, mode);
        }

        public ulong GetChannelPosition(ChannelHandle handle, LengthMode mode)
        {
            return BASS_ChannelGetPosition(handle, mode);
        }

        public ulong GetChannelPosition(MusicHandle handle, LengthMode mode)
        {
            return BASS_ChannelGetPosition(handle, mode);
        }

        public ulong GetChannelPosition(StreamHandle handle, LengthMode mode)
        {
            return BASS_ChannelGetPosition(handle, mode);
        }

        public ulong GetChannelPosition(RecordHandle handle, LengthMode mode)
        {
            return BASS_ChannelGetPosition(handle, mode);
        }

        public ChannelStatus GetChannelStatus(ChannelHandle handle)
        {
            return BASS_ChannelIsActive(handle);
        }

        public ChannelStatus GetChannelStatus(MusicHandle handle)
        {
            return BASS_ChannelIsActive(handle);
        }

        public ChannelStatus GetChannelStatus(StreamHandle handle)
        {
            return BASS_ChannelIsActive(handle);
        }

        public ChannelStatus GetChannelStatus(RecordHandle handle)
        {
            return BASS_ChannelIsActive(handle);
        }

        public double ConvertBytesToSeconds(ChannelHandle handle, ulong position)
        {
            return BASS_ChannelBytes2Seconds(handle, position);
        }

        public double ConvertBytesToSeconds(MusicHandle handle, ulong position)
        {
            return BASS_ChannelBytes2Seconds(handle, position);
        }

        public double ConvertBytesToSeconds(StreamHandle handle, ulong position)
        {
            return BASS_ChannelBytes2Seconds(handle, position);
        }

        public double ConvertBytesToSeconds(RecordHandle handle, ulong position)
        {
            return BASS_ChannelBytes2Seconds(handle, position);
        }

        public double ConvertBytesToSeconds(SampleHandle handle, ulong position)
        {
            return BASS_ChannelBytes2Seconds(handle, position);
        }

        public static BassError GetLastErrorCode()
        {
            return BASS_ErrorGetCode();
        }

        public uint GetConfig(BassConfig option)
        {
            var value = BASS_GetConfig(option);
            if (value == 4294967295U)
            {
                throw new BassException("GetConfig failed");
            }

            return value;
        }

        public void SetConfig(BassConfig option, uint value)
        {
            if (!BASS_SetConfig(option, value))
            {
                throw new BassException("SetConfig failed");
            }
        }

        public SyncHandle SetSync(MusicHandle handle, BassSync type, ulong param, SyncProc callback, IntPtr user)
        {
            var syncHandle = BASS_ChannelSetSync(handle, type, param, callback, user);
            if (syncHandle == SyncHandle.Null)
            {
                throw new BassException("SetSync failed");
            }

            return syncHandle;
        }

        public SyncHandle SetSync(StreamHandle handle, BassSync type, ulong param, SyncProc callback, IntPtr user)
        {
            var syncHandle = BASS_ChannelSetSync(handle, type, param, callback, user);
            if (syncHandle == SyncHandle.Null)
            {
                throw new BassException("SetSync failed");
            }

            return syncHandle;
        }

        public SyncHandle SetSync(RecordHandle handle, BassSync type, ulong param, SyncProc callback, IntPtr user)
        {
            var syncHandle = BASS_ChannelSetSync(handle, type, param, callback, user);
            if (syncHandle == SyncHandle.Null)
            {
                throw new BassException("SetSync failed");
            }

            return syncHandle;
        }

        public void RemoveSync(MusicHandle handle, SyncHandle sync)
        {
            if (!BASS_ChannelRemoveSync(handle, sync))
            {
                throw new BassException("RemoveSync failed");
            }
        }

        public void RemoveSync(StreamHandle handle, SyncHandle sync)
        {
            if (!BASS_ChannelRemoveSync(handle, sync))
            {
                throw new BassException("RemoveSync failed");
            }
        }

        public void RemoveSync(RecordHandle handle, SyncHandle sync)
        {
            if (!BASS_ChannelRemoveSync(handle, sync))
            {
                throw new BassException("RemoveSync failed");
            }
        }

        public bool IsHandleValid(ChannelHandle handle)
        {
            return handle != ChannelHandle.Null && (GetChannelStatus(handle) != ChannelStatus.Stopped || GetLastErrorCode() == BassError.Ok);
        }

        public bool IsHandleValid(MusicHandle handle)
        {
            return handle != MusicHandle.Null && (GetChannelStatus(handle) != ChannelStatus.Stopped || GetLastErrorCode() == BassError.Ok);
        }

        public bool IsHandleValid(StreamHandle handle)
        {
            return handle != StreamHandle.Null && (GetChannelStatus(handle) != ChannelStatus.Stopped || GetLastErrorCode() == BassError.Ok);
        }

        public bool IsHandleValid(RecordHandle handle)
        {
            return handle != RecordHandle.Null && (GetChannelStatus(handle) != ChannelStatus.Stopped || GetLastErrorCode() == BassError.Ok);
        }

        [DllImport(Bass)]
        private static extern PluginHandle BASS_PluginLoad([MarshalAs(StringMarshal)] string file, uint flags);

        [DllImport(Bass)]
        private static extern bool BASS_PluginFree(PluginHandle handle);

        [DllImport(Bass)]
        private static extern unsafe BassPluginInfo* BASS_PluginGetInfo(PluginHandle handle);

        [DllImport(Bass)]
        private static extern bool BASS_Init(int device, uint frequency, BassInitFlags flags, IntPtr window, IntPtr clsid);

        [DllImport(Bass)]
        private static extern bool BASS_Free();

        [DllImport(Bass)]
        private static extern bool BASS_Start();

        [DllImport(Bass)]
        private static extern bool BASS_IsStarted();

        [DllImport(Bass)]
        private static extern bool BASS_Stop();

        [DllImport(Bass)]
        private static extern bool BASS_GetInfo(out BassInfo info);

        [DllImport(Bass)]
        private static extern bool BASS_StreamFree(StreamHandle handle);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelPlay(ChannelHandle handle, bool restart = false);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelPlay(MusicHandle handle, bool restart = false);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelPlay(StreamHandle handle, bool restart = false);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelPlay(RecordHandle handle, bool restart = false);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelPause(ChannelHandle handle);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelPause(MusicHandle handle);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelPause(StreamHandle handle);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelPause(RecordHandle handle);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelStop(ChannelHandle handle);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelStop(MusicHandle handle);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelStop(StreamHandle handle);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelStop(RecordHandle handle);

        [DllImport(Bass)]
        private static extern ChannelStatus BASS_ChannelIsActive(ChannelHandle handle);

        [DllImport(Bass)]
        private static extern ChannelStatus BASS_ChannelIsActive(MusicHandle handle);

        [DllImport(Bass)]
        private static extern ChannelStatus BASS_ChannelIsActive(StreamHandle handle);

        [DllImport(Bass)]
        private static extern ChannelStatus BASS_ChannelIsActive(RecordHandle handle);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelGetInfo(ChannelHandle handle, out BassChannelInfo info);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelGetInfo(MusicHandle handle, out BassChannelInfo info);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelGetInfo(StreamHandle handle, out BassChannelInfo info);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelGetInfo(RecordHandle handle, out BassChannelInfo info);

        [DllImport(Bass)]
        private static extern ulong BASS_ChannelGetLength(ChannelHandle handle, LengthMode mode);

        [DllImport(Bass)]
        private static extern ulong BASS_ChannelGetLength(MusicHandle handle, LengthMode mode);

        [DllImport(Bass)]
        private static extern ulong BASS_ChannelGetLength(StreamHandle handle, LengthMode mode);

        [DllImport(Bass)]
        private static extern ulong BASS_ChannelGetLength(SampleHandle handle, LengthMode mode);

        [DllImport(Bass)]
        private static extern ulong BASS_ChannelGetPosition(ChannelHandle handle, LengthMode mode);

        [DllImport(Bass)]
        private static extern ulong BASS_ChannelGetPosition(MusicHandle handle, LengthMode mode);

        [DllImport(Bass)]
        private static extern ulong BASS_ChannelGetPosition(StreamHandle handle, LengthMode mode);

        [DllImport(Bass)]
        private static extern ulong BASS_ChannelGetPosition(RecordHandle handle, LengthMode mode);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelGetAttribute(ChannelHandle handle, ChannelAttribute attribute, out float value);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelGetAttribute(MusicHandle handle, ChannelAttribute attribute, out float value);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelGetAttribute(StreamHandle handle, ChannelAttribute attribute, out float value);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelGetAttribute(RecordHandle handle, ChannelAttribute attribute, out float value);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelSetAttribute(ChannelHandle handle, ChannelAttribute attribute, float value);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelSetAttribute(MusicHandle handle, ChannelAttribute attribute, float value);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelSetAttribute(StreamHandle handle, ChannelAttribute attribute, float value);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelSetAttribute(RecordHandle handle, ChannelAttribute attribute, float value);

        [DllImport(Bass)]
        private static extern double BASS_ChannelBytes2Seconds(ChannelHandle handle, ulong position);

        [DllImport(Bass)]
        private static extern double BASS_ChannelBytes2Seconds(MusicHandle handle, ulong position);

        [DllImport(Bass)]
        private static extern double BASS_ChannelBytes2Seconds(StreamHandle handle, ulong position);

        [DllImport(Bass)]
        private static extern double BASS_ChannelBytes2Seconds(RecordHandle handle, ulong position);

        [DllImport(Bass)]
        private static extern double BASS_ChannelBytes2Seconds(SampleHandle handle, ulong position);

        [DllImport(Bass)]
        private static extern BassError BASS_ErrorGetCode();

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
        private static extern StreamHandle BASS_StreamCreateFile(bool memory, [MarshalAs(StringMarshal)] string file, ulong offset, ulong length, StreamFlags flags);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void DownloadProc(IntPtr buffer, uint length, IntPtr user);

        [DllImport(Bass)]
        private static extern StreamHandle BASS_StreamCreateURL([MarshalAs(StringMarshal)] string url, uint offset, StreamFlags flags, DownloadProc proc, IntPtr user);

        [DllImport(Bass)]
        private static extern StreamHandle BASS_StreamCreate(uint freq, uint chans, StreamFlags flags, IntPtr proc, IntPtr user);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate void SyncProc(uint handle, uint channel, uint data, IntPtr user);

        [DllImport(Bass)]
        private static extern SyncHandle BASS_ChannelSetSync(MusicHandle handle, BassSync type, ulong param, SyncProc proc, IntPtr user);

        [DllImport(Bass)]
        private static extern SyncHandle BASS_ChannelSetSync(StreamHandle handle, BassSync type, ulong param, SyncProc proc, IntPtr user);

        [DllImport(Bass)]
        private static extern SyncHandle BASS_ChannelSetSync(RecordHandle handle, BassSync type, ulong param, SyncProc proc, IntPtr user);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelRemoveSync(MusicHandle handle, SyncHandle sync);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelRemoveSync(StreamHandle handle, SyncHandle sync);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelRemoveSync(RecordHandle handle, SyncHandle sync);
    }
}
