using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AudioSensei.Bass.Native
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

        public IEnumerable<string> ListSupportedFormats()
        {
            for (int i = 0; i < formatc; i++)
            {
                var f = GetFormatAt(i);
                yield return $"Format: {Marshal.PtrToStringUTF8(f.name)} - extensions: {Marshal.PtrToStringUTF8(f.exts)}";
            }
        }
    }
}
