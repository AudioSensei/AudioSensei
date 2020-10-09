using System;
using System.Runtime.InteropServices;

namespace AudioSensei.Bass
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly unsafe struct BassPluginInfo
    {
        public readonly BassVersion version;
        public readonly uint formatc;
        public readonly BassPluginFormat* formats;

        public BassPluginFormat GetFormatAt(int index)
        {
            if (index >= formatc)
            {
                throw new IndexOutOfRangeException();
            }
            return formats[index];
        }
    }
}
