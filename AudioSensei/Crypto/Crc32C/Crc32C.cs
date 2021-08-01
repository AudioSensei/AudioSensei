using System;
using System.Buffers;
using System.IO;

namespace AudioSensei.Crypto.Crc32C
{
    public abstract class Crc32C
    {
        public static Crc32C Create()
        {
            if (Crc32CSse64.Supported)
            {
                return new Crc32CSse64();
            }

            if (Crc32CSse32.Supported)
            {
                return new Crc32CSse32();
            }

            if (Crc32CArm64.Supported)
            {
                return new Crc32CArm64();
            }

            if (Crc32CArm32.Supported)
            {
                return new Crc32CArm32();
            }

            return new Crc32CManaged();
        }

        public virtual uint Calculate(ReadOnlySpan<byte> data)
        {
            return Append(uint.MaxValue, data) ^ uint.MaxValue;
        }

        public virtual uint Calculate(Stream stream)
        {
            byte[] buffer = null;
            try
            {
                buffer = ArrayPool<byte>.Shared.Rent(4096);
                // allign to ulongs for better speed
                Span<byte> data = buffer.AsSpan(0, buffer.Length / sizeof(ulong) * sizeof(ulong));
                uint crc = uint.MaxValue;
                int count;
                while ((count = stream.Read(data)) != 0)
                {
                    crc = Append(crc, data.Slice(0, count));
                }

                return crc ^ uint.MaxValue;
            }
            finally
            {
                if (buffer != null)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }

        protected abstract uint Append(uint crc, ReadOnlySpan<byte> data);
    }
}
