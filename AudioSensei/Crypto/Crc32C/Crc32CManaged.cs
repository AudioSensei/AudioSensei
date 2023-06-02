using System;

namespace AudioSensei.Crypto.Crc32C
{
    internal sealed class Crc32CManaged : Crc32C
    {
        private readonly uint[] _table;

        internal Crc32CManaged()
        {
            const uint polynomial = 0x82F63B78;
            _table = new uint[16 * 256];
            for (uint i = 0; i < 256; i++)
            {
                uint res = i;
                for (int t = 0; t < 16; t++)
                {
                    for (int k = 0; k < 8; k++) res = (res & 1) == 1 ? polynomial ^ (res >> 1) : res >> 1;
                    _table[t * 256 + i] = res;
                }
            }
        }

        protected override uint Append(uint crc, ReadOnlySpan<byte> data)
        {
            int processed;
            for (processed = 0; processed < data.Length - 16; processed += 16)
            {
                crc = _table[15 * 256 + ((crc ^ data[processed]) & 0xff)]
                  ^ _table[14 * 256 + (((crc >> 8) ^ data[processed + 1]) & 0xff)]
                  ^ _table[13 * 256 + (((crc >> 16) ^ data[processed + 2]) & 0xff)]
                  ^ _table[12 * 256 + (((crc >> 24) ^ data[processed + 3]) & 0xff)]
                  ^ _table[11 * 256 + data[processed + 4]]
                  ^ _table[10 * 256 + data[processed + 5]]
                  ^ _table[9 * 256 + data[processed + 6]]
                  ^ _table[8 * 256 + data[processed + 7]]
                  ^ _table[7 * 256 + data[processed + 8]]
                  ^ _table[6 * 256 + data[processed + 9]]
                  ^ _table[5 * 256 + data[processed + 10]]
                  ^ _table[4 * 256 + data[processed + 11]]
                  ^ _table[3 * 256 + data[processed + 12]]
                  ^ _table[2 * 256 + data[processed + 13]]
                  ^ _table[1 * 256 + data[processed + 14]]
                  ^ _table[0 + data[processed + 15]];
            }

            for (int i = processed; i < data.Length; i++)
            {
                crc = _table[(crc ^ data[i]) & 0xff] ^ crc >> 8;
            }

            return crc;
        }
    }
}
