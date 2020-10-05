using System;
using System.Runtime.InteropServices;

namespace AudioSensei
{
    internal static unsafe class UnsafeHelpers
    {
        public static int Strlen(byte* data)
        {
            int i = 0;
            while (data[i++] != 0) { }
            return i;
        }

        public static int Strlen(byte* data, int maxSize)
        {
            for (int i = 0; i < maxSize; i++)
            {
                if (data[i] == 0)
                {
                    return i;
                }
            }

            return maxSize;
        }

        // ReSharper disable once InconsistentNaming
        public static string PtrToStringUTF8(IntPtr data, int maxSize)
        {
            return data == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(data, Strlen((byte*)data.ToPointer(), maxSize));
        }
    }
}
