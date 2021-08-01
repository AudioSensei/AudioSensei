using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AudioSensei.Crypto
{
    public class SecureRandom : Random
    {
        public override int Next()
        {
            return RandomNumberGenerator.GetInt32(int.MaxValue);
        }

        public override int Next(int maxValue)
        {
            return RandomNumberGenerator.GetInt32(maxValue);
        }

        public override int Next(int minValue, int maxValue)
        {
            return RandomNumberGenerator.GetInt32(minValue, maxValue);
        }

        public override void NextBytes(byte[] buffer)
        {
            RandomNumberGenerator.Fill(buffer);
        }

        public override void NextBytes(Span<byte> buffer)
        {
            RandomNumberGenerator.Fill(buffer);
        }

        public override double NextDouble()
        {
            return Sample();
        }

        protected override double Sample()
        {
            Span<ulong> buffer = stackalloc ulong[1];
            double d;
            do
            {
                RandomNumberGenerator.Fill(MemoryMarshal.AsBytes(buffer));
                const double max = ulong.MaxValue;
                d = buffer[1] / max;
            } while (d < 0d || d >= 1d);
            return d;
        }
    }
}
