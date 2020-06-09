using System;
using System.Runtime.InteropServices;

namespace AudioSensei.Bass
{
    internal class BassNative
    {
        private const string Bass = "bass";

        [DllImport(Bass, EntryPoint = "BASS_Init")]
        internal static extern bool Init(int device, uint frequency, uint flags, IntPtr win, IntPtr clsid);

        [DllImport(Bass, EntryPoint = "BASS_Free")]
        internal static extern bool Free();

        [DllImport(Bass, EntryPoint = "BASS_StreamFree")]
        internal static extern bool StreamFree(uint handle);

        [DllImport(Bass, EntryPoint = "BASS_ChannelPlay")]
        internal static extern bool ChannelPlay(uint handle, bool restart = false);

        [DllImport(Bass, EntryPoint = "BASS_ChannelPause")]
        internal static extern bool ChannelPause(uint handle);

        [DllImport(Bass, EntryPoint = "BASS_ChannelStop")]
        internal static extern bool ChannelStop(uint handle);

        [DllImport(Bass, EntryPoint = "BASS_ChannelIsActive")]
        internal static extern uint ChannelIsActive(uint handle);

        [DllImport(Bass, EntryPoint = "BASS_ChannelGetLength")]
        internal static extern ulong ChannelGetLength(uint handle, uint mode);

        [DllImport(Bass, EntryPoint = "BASS_ChannelGetPosition")]
        internal static extern ulong ChannelGetPosition(uint handle, uint mode);

        [DllImport(Bass, EntryPoint = "BASS_ChannelGetAttribute")]
        internal static extern bool ChannelGetAttribute(uint handle, uint attribute, ref float value);

        [DllImport(Bass, EntryPoint = "BASS_ChannelSetAttribute")]
        internal static extern bool ChannelSetAttribute(uint handle, uint attribute, float value);

        [DllImport(Bass, EntryPoint = "BASS_ChannelBytes2Seconds")]
        internal static extern double ChanbnelBytes2Seconds(uint handle, ulong position);

        [DllImport(Bass, EntryPoint = "BASS_ErrorGetCode")]
        internal static extern uint GetLastError();

        [DllImport(Bass, EntryPoint = "BASS_StreamCreateFile")]
        internal static extern uint CreateFileStream(bool memory, [MarshalAs(UnmanagedType.LPWStr)] string file, ulong offset, ulong length, uint flags);
    }
}