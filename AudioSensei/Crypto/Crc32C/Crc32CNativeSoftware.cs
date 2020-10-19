using System;

namespace AudioSensei.Crypto.Crc32C
{
    internal sealed class Crc32CNativeSoftware : Crc32C
    {
        public static bool Supported => Crc32CNative.Supported;

        internal Crc32CNativeSoftware()
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
                crc = Crc32CNative.AppendSoftware(crc, bytes, (uint)data.Length);
            }

            return crc;
        }
    }
}
