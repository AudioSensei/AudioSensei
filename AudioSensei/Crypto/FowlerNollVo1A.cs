using System.Text;

namespace AudioSensei.Crypto
{
    public static class FowlerNollVo1A
    {
        public static ulong GetHash(byte[] data)
        {
            ulong hash = 14695981039346656037UL; //offset
            for (var i = 0; i < data.Length; i++)
            {
                hash ^= data[i];
                hash *= 1099511628211UL; //prime
            }

            return hash;
        }

        public static ulong GetHash(string data)
        {
            return GetHash(Encoding.UTF8.GetBytes(data));
        }
    }
}
