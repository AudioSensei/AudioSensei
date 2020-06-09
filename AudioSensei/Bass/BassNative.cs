using System;
using System.Runtime.InteropServices;

namespace AudioSensei.Bass
{
    internal class BassNative
    {
        private const string Bass = "bass";

        public static void Init()
        {
            if (!BASS_Init(-1, 44000, 0, IntPtr.Zero, IntPtr.Zero))
            {
                throw new BassException($"Init failed");
            }
        }

        public static void Free()
        {
            if (!BASS_Free())
            {
                throw new BassException($"Free failed");
            }
        }

        public static uint CreateStreamFromFile(string filePath)
        {
            var handle = BASS_StreamCreateFile(false, filePath, 0, 0, unchecked((uint) int.MinValue));

            if (handle == 0)
            {
                throw new BassException("StreamCreateFile failed");
            }

            return handle;
        }

        public static void FreeStream(uint handle)
        {
            if (!BASS_StreamFree(handle))
            {
                throw new BassException("StreamFree failed");
            }
        }

        public static void PlayChannel(uint handle, bool restart = false)
        {
            if (!BASS_ChannelPlay(handle, restart))
            {
                throw new BassException($"ChannelPlay failed");
            }
        }

        public static void PauseChannel(uint handle)
        {
            if (!BASS_ChannelPause(handle))
            {
                throw new BassException($"ChannelPause failed");
            }
        }

        public static void StopChannel(uint handle)
        {
            if (!BASS_ChannelStop(handle))
            {
                throw new BassException($"ChannelStop failed");
            }
        }

        public static float GetChannelAttribute(uint handle, ChannelAttribute attribute)
        {
            var value = 0.0f;

            if (!BASS_ChannelGetAttribute(handle, (uint) attribute, ref value))
            {
                throw new BassException($"ChannelGetAttribute failed");
            }

            return value;
        }

        public static void SetChannelAttribute(uint handle, ChannelAttribute attribute, float value)
        {
            if (!BASS_ChannelSetAttribute(handle, (uint) attribute, value))
            {
                throw new BassException($"ChannelSetAttribute failed");
            }
        }

        public static ulong GetChannelLength(uint handle, LengthMode mode)
        {
            return BASS_ChannelGetLength(handle, (uint) mode);
        }

        public static ulong GetChannelPosition(uint handle, LengthMode mode)
        {
            return BASS_ChannelGetPosition(handle, (uint) mode);
        }

        public static ChannelStatus GetChannelStatus(uint handle)
        {
            return (ChannelStatus) BASS_ChannelIsActive(handle);
        }

        public static double ConvertBytesToSeconds(uint handle, ulong position)
        {
            return BASS_ChannelBytes2Seconds(handle, position);
        }

        public static uint GetLastErrorCode()
        {
            return BASS_ErrorGetCode();
        }

        [DllImport(Bass)]
        private static extern bool BASS_Init(int device, uint frequency, uint flags, IntPtr window, IntPtr clsid);

        [DllImport(Bass)]
        private static extern bool BASS_Free();

        [DllImport(Bass)]
        private static extern bool BASS_StreamFree(uint handle);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelPlay(uint handle, bool restart = false);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelPause(uint handle);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelStop(uint handle);

        [DllImport(Bass)]
        private static extern uint BASS_ChannelIsActive(uint handle);

        [DllImport(Bass)]
        private static extern ulong BASS_ChannelGetLength(uint handle, uint mode);

        [DllImport(Bass)]
        private static extern ulong BASS_ChannelGetPosition(uint handle, uint mode);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelGetAttribute(uint handle, uint attribute, ref float value);

        [DllImport(Bass)]
        private static extern bool BASS_ChannelSetAttribute(uint handle, uint attribute, float value);

        [DllImport(Bass)]
        private static extern double BASS_ChannelBytes2Seconds(uint handle, ulong position);

        [DllImport(Bass)]
        private static extern uint BASS_ErrorGetCode();

        [DllImport(Bass)]
        private static extern uint BASS_StreamCreateFile(bool memory, [MarshalAs(UnmanagedType.LPWStr)] string file, ulong offset, ulong length, uint flags);
    }
}