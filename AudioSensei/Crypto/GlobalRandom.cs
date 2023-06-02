using System;
using System.Security.Cryptography;

namespace AudioSensei.Crypto
{
    public static class GlobalRandom
    {
        private static readonly Random Random = new(RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue));
        private static readonly object RandomLock = new();

        public static int Next()
        {
            lock (RandomLock)
            {
                return Random.Next();
            }
        }

        public static int Next(int maxValue)
        {
            lock (RandomLock)
            {
                return Random.Next(maxValue);
            }
        }

        public static int Next(int minValue, int maxValue)
        {
            lock (RandomLock)
            {
                return Random.Next(minValue, maxValue);
            }
        }

        public static void NextBytes(byte[] buffer)
        {
            lock (RandomLock)
            {
                Random.NextBytes(buffer);
            }
        }

        public static void NextBytes(Span<byte> buffer)
        {
            lock (RandomLock)
            {
                Random.NextBytes(buffer);
            }
        }

        public static double NextDouble()
        {
            lock (RandomLock)
            {
                return Random.NextDouble();
            }
        }
    }
}
