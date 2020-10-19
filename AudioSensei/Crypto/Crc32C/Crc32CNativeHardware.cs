using System;
using System.Collections.Generic;
using System.Text;

namespace AudioSensei.Crypto.Crc32C
{
    internal sealed class Crc32CNativeHardware : Crc32C
    {
        public static bool Supported => Crc32CNative.Supported && Crc32CNative.Hardware;

        internal Crc32CNativeHardware()
        {
            if (!Supported)
            {
                throw new PlatformNotSupportedException();
            }
        }

        protected override unsafe uint Append(uint crc, ReadOnlySpan<byte> data)
        {
            fixed (byte* bytes = data)
            {
                crc = Crc32CNative.AppendHardware(crc, bytes, (uint)data.Length);
            }

            return crc;
        }
    }
}
