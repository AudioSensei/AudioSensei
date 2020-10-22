using System.Runtime.InteropServices;

namespace AudioSensei.Bass.BassCd
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct CdToc
    {
        public ushort size;      // size of TOC
        public byte first;     // first track
        public byte last;      // last track
        [MarshalAs(UnmanagedType.U8)]
        public fixed ulong tracks[100]; // up to 100 tracks
    }
}
