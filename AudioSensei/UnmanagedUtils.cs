using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace AudioSensei
{
    public static unsafe class UnmanagedUtils
    {
        public static int Strlen(byte* data, int maxSize = int.MaxValue)
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

        public static void HiLoWord(uint value, out ushort hiword, out ushort loword)
        {
            hiword = (ushort)(value >> sizeof(ushort) * 8);
            loword = (ushort)value;
        }

        public static void HiLoByte(ushort value, out byte hibyte, out byte lobyte)
        {
            hibyte = (byte)(value >> sizeof(byte) * 8);
            lobyte = (byte)value;
        }

        public static ushort MakeWord(byte hibyte, byte lobyte)
        {
            return (ushort)((ushort)(hibyte << (sizeof(byte) * 8)) | lobyte);
        }

        public static uint MakeLong(ushort hiword, ushort loword)
        {
            return (uint)(hiword << (sizeof(ushort) * 8)) | loword;
        }

        public static T* Alloc<T>(int count) where T : unmanaged
        {
            return (T*)Marshal.AllocCoTaskMem(sizeof(T) * count);
        }

        public static void Free(void* ptr)
        {
            if (ptr != null)
                Marshal.FreeCoTaskMem(new IntPtr(ptr));
        }
    }
}
