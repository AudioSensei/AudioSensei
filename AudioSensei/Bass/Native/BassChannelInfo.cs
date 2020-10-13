using System;
using System.Runtime.InteropServices;
using AudioSensei.Bass.Native.Handles;

namespace AudioSensei.Bass.Native
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct BassChannelInfo
    {
        public readonly uint freq;
        public readonly uint chans;
        public readonly StreamFlags flags;
        public readonly ChannelType ctype;
        public readonly uint orgies;
        public readonly PluginHandle plugin;
        public readonly uint sample;
        private readonly IntPtr filename;

        public string FileName => flags.HasFlag(StreamFlags.Unicode) ? Marshal.PtrToStringUni(filename) : Marshal.PtrToStringUTF8(filename);
    }
}
