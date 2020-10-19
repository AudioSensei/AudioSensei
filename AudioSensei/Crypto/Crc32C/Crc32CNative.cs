using System.Runtime.InteropServices;

namespace AudioSensei.Crypto.Crc32C
{
    internal static unsafe class Crc32CNative
    {
        private const string Library = "crc32c";

        public static readonly bool Supported;
        public static readonly bool Hardware;

        static Crc32CNative()
        {
            try
            {
                Hardware = CheckHardwareSupport() != 0;
                Supported = true;
            }
            catch
            {
                Supported = false;
            }
        }

        /// <summary>
        /// Software fallback version of CRC-32C (Castagnoli) checksum.
        /// </summary>
        /// <param name="crc">Initial CRC value. Typically it's 0. You can supply non-trivial initial value here. Initial value can be used to chain CRC from multiple buffers.</param>
        /// <param name="input">Data to be put through the CRC algorithm.</param>
        /// <param name="length">Length of the data in the input buffer.</param>
        /// <returns>Calculated hash</returns>
        [DllImport(Library, EntryPoint = "crc32c_append_sw")]
        internal static extern uint AppendSoftware(uint crc, byte* input, uint length);

        /// <summary>
        /// Hardware version of CRC-32C (Castagnoli) checksum. Will fail, if CPU does not support related instructions. Use a crc32c_append version instead of.
        /// </summary>
        /// <param name="crc">Initial CRC value. Typically it's 0. You can supply non-trivial initial value here. Initial value can be used to chain CRC from multiple buffers.</param>
        /// <param name="input">Data to be put through the CRC algorithm.</param>
        /// <param name="length">Length of the data in the input buffer.</param>
        /// <returns>Calculated hash</returns>
        [DllImport(Library, EntryPoint = "crc32c_append_hw")]
        internal static extern uint AppendHardware(uint crc, byte* input, uint length);

        /// <summary>
        /// Checks is hardware version of CRC-32C is available.
        /// </summary>
        /// <returns>0 when not supported, else supported</returns>
        [DllImport(Library, EntryPoint = "crc32c_hw_available")]
        private static extern int CheckHardwareSupport();
    }
}
