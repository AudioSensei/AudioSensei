using System.Runtime.InteropServices;

namespace AudioSensei.Bass.BassCd
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct BassCdInfo
    {
        [MarshalAs(UnmanagedType.LPUTF8Str)]
        public string vendor;
        [MarshalAs(UnmanagedType.LPUTF8Str)]
        public string product;
        [MarshalAs(UnmanagedType.LPUTF8Str)]
        public string rev;
        public int letter;
        public uint rwflags;
        public bool canopen;
        public bool canlock;
        public uint maxspeed;
        public uint cache;
        public bool cdtext;
    }
}
