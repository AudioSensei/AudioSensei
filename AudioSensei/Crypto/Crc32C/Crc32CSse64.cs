using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace AudioSensei.Crypto.Crc32C
{
    internal sealed class Crc32CSse64 : Crc32C
    {
        public static bool Supported => Sse42.IsSupported && Sse42.X64.IsSupported;

        internal Crc32CSse64()
        {
            if (!Supported)
            {
                throw new PlatformNotSupportedException();
            }
        }

        protected override uint Append(uint crc, ReadOnlySpan<byte> data)
        {
            int processed = 0;
            if (data.Length > sizeof(ulong))
            {
                processed = data.Length / sizeof(ulong) * sizeof(ulong);
                ReadOnlySpan<ulong> ulongs = MemoryMarshal.Cast<byte, ulong>(data.Slice(0, processed));
                ulong crclong = crc;
                for (int i = 0; i < ulongs.Length; i++)
                {
                    crclong = Sse42.X64.Crc32(crclong, ulongs[i]);
                }

                crc = (uint)crclong;
            }
            else if (data.Length > sizeof(uint))
            {
                processed = data.Length / sizeof(uint) * sizeof(uint);
                ReadOnlySpan<uint> uints = MemoryMarshal.Cast<byte, uint>(data.Slice(0, processed));
                for (int i = 0; i < uints.Length; i++)
                {
                    crc = Sse42.Crc32(crc, uints[i]);
                }
            }

            for (int i = processed; i < data.Length; i++)
            {
                crc = Sse42.Crc32(crc, data[i]);
            }

            return crc;
        }
    }
}
