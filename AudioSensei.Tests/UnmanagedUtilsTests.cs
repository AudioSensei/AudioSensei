using Xunit;

namespace AudioSensei.Tests
{
    public class UnmanagedUtilsTests
    {
        [Theory]
        [InlineData(new byte[] { 0 }, 5000, 0)]
        [InlineData(new byte[] { 1, 2, 3, 4, 5, 0 }, 5000, 5)]
        [InlineData(new byte[] { 1, 2, 0, 4, 5, 0 }, 5000, 2)]
        [InlineData(new byte[] { 1, 2, 3, 4, 5, 0 }, 3, 3)]
#pragma warning disable xUnit1026 // Incorrect warning
        public unsafe void TestStrlen(byte[] data, int limit, int result)
#pragma warning restore xUnit1026
        {
            fixed (byte* buffer = data)
            {
                int strlen = UnmanagedUtils.Strlen(buffer, limit);
                Assert.Equal(result, strlen);
                for (int i = 0; i < strlen; i++)
                {
                    Assert.NotEqual(0, buffer[i]);
                }
            }
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(uint.MaxValue, ushort.MaxValue, ushort.MaxValue)]
        [InlineData(0b00000000111111110000000011111111, 0b0000000011111111, 0b0000000011111111)]
        public void TestHiLoWordMakeLong(uint value, ushort hiword, ushort loword)
        {
            UnmanagedUtils.HiLoWord(value, out var hivalue, out var lovalue);
            Assert.Equal(hiword, hivalue);
            Assert.Equal(loword, lovalue);
            Assert.Equal(value, UnmanagedUtils.MakeLong(hivalue, lovalue));
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(ushort.MaxValue, byte.MaxValue, byte.MaxValue)]
        [InlineData(0b0000111100001111, 0b00001111, 0b00001111)]
        public void TestHiLoByteMakeWord(ushort value, byte hiword, byte loword)
        {
            UnmanagedUtils.HiLoByte(value, out var hivalue, out var lovalue);
            Assert.Equal(hiword, hivalue);
            Assert.Equal(loword, lovalue);
            Assert.Equal(value, UnmanagedUtils.MakeWord(hivalue, lovalue));
        }
    }
}
