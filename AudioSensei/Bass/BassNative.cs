using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AudioSensei.Bass
{
    internal static class BassNative
    {
        private const string Bass = "bass";

        private static readonly object LoadLock = new object();

        public static void Initialize(int device = -1, uint frequency = 44000)
        {
            lock (LoadLock)
            {
                string filter;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    filter = "bass*.dll";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    filter = "libbass*.so";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    filter = "libbass*.dylib";
                else
                    throw new PlatformNotSupportedException();

                foreach (var file in Directory.EnumerateFiles("BassPlugins", filter))
                {
                    if (LoadPlugin(file, 0) == 0)
                        throw new BassException($"Loading {file} as plugin failed");
                }

                var window = IntPtr.Zero;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    window = GetActiveWindow();
                }

                if (!BASS_Init(device, frequency, 0, window, IntPtr.Zero))
                {
                    throw new BassException("Init failed");
                }
            }
        }

        [DllImport("user32")]
        private static extern IntPtr GetActiveWindow();

        public static void Free()
        {
            lock (LoadLock)
            {
                UnloadPlugin(0);
                if (!BASS_Free())
                {
                    throw new BassException("Free failed");
                }
            }
        }

        public static BassHandle CreateStreamFromFile(string filePath)
        {
            var handle = BASS_StreamCreateFile(false, filePath, 0, 0, StreamFlags.AsyncFile | StreamFlags.Unicode | StreamFlags.StreamPrescan | StreamFlags.StreamAutofree);

            if (handle == BassHandle.Null)
            {
                throw new BassException("StreamCreateFile failed");
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

        private static BassHandle BASS_StreamCreateFile(bool memory, string file, ulong offset, ulong length, StreamFlags flags)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? BASS_StreamCreateFileWindows(memory, file, offset, length, flags | StreamFlags.Unicode)
                : BASS_StreamCreateFileUnix(memory, file, offset, length, flags);
        }

        private static uint LoadPlugin(string file, uint flags)
        {
            const uint unicode = 0x80000000;
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? LoadPluginWindows(file, flags | unicode)
                : LoadPluginUnix(file, flags);
        }

        [DllImport(Bass, EntryPoint = "BASS_PluginLoad")]
        private static extern uint LoadPluginWindows([MarshalAs(UnmanagedType.LPWStr)] string file, uint flags);

        [DllImport(Bass, EntryPoint = "BASS_PluginLoad")]
        private static extern uint LoadPluginUnix([MarshalAs(UnmanagedType.LPUTF8Str)] string file, uint flags);

        [DllImport(Bass, EntryPoint = "BASS_PluginFree")]
        private static extern bool UnloadPlugin(uint handle);

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

        [DllImport(Bass, EntryPoint = "BASS_StreamCreateFile")]
        private static extern BassHandle BASS_StreamCreateFileWindows(bool memory, [MarshalAs(UnmanagedType.LPWStr)] string file, ulong offset, ulong length, StreamFlags flags);

        [DllImport(Bass, EntryPoint = "BASS_StreamCreateFile")]
        private static extern BassHandle BASS_StreamCreateFileUnix(bool memory, [MarshalAs(UnmanagedType.LPUTF8Str)] string file, ulong offset, ulong length, StreamFlags flags);
    }
}
