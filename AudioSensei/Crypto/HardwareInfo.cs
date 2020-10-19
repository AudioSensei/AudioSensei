using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using Aes = System.Runtime.Intrinsics.X86.Aes;

namespace AudioSensei.Crypto
{
    public static class HardwareInfo
    {
        [Flags]
        public enum HardwareSupport : ulong
        {
            None = 0UL,

            #region X86
            // 0-27
            X86 = 1UL << 0,
            X86Base = 1UL << 1,
            AesNi = 1UL << 2,
            Avx = 1UL << 3,
            Avx2 = 1UL << 4,
            Bmi1 = 1UL << 5,
            Bmi2 = 1UL << 6,
            Fma = 1UL << 7,
            Lzcnt = 1UL << 8,
            Pclmulqdq = 1UL << 9,
            Popcnt = 1UL << 10,
            Sse = 1UL << 11,
            Sse2 = 1UL << 12,
            Sse3 = 1UL << 13,
            Sse41 = 1UL << 14,
            Sse42 = 1UL << 15,
            Ssse3 = 1UL << 16,
            #endregion

            #region ARM
            // 28-55
            Arm = 1UL << 28,
            ArmBase = 1UL << 29,
            AdvSimd = 1UL << 30,
            Aes = 1UL << 31,
            Crc32 = 1UL << 32,
            Dp = 1UL << 33,
            Rdm = 1UL << 34,
            Sha1 = 1UL << 35,
            Sha256 = 1UL << 36,
            #endregion

            #region Other
            // 56-63
            Bit64 = 1UL << 63
            #endregion
        }

        public static HardwareSupport GetHardwareSupport()
        {
            HardwareSupport sharedFlags = HardwareSupport.None;
            if (Environment.Is64BitProcess)
                sharedFlags |= HardwareSupport.Bit64;
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X86:
                case Architecture.X64:
                    return sharedFlags | GetX86Flags();
                case Architecture.Arm:
                case Architecture.Arm64:
                    return sharedFlags | GetArmFlags();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static HardwareSupport GetX86Flags()
        {
            HardwareSupport flags = HardwareSupport.X86;
            if (X86Base.IsSupported)
                flags |= HardwareSupport.X86Base;
            if (Aes.IsSupported)
                flags |= HardwareSupport.AesNi;
            if (Avx.IsSupported)
                flags |= HardwareSupport.Avx;
            if (Avx2.IsSupported)
                flags |= HardwareSupport.Avx2;
            if (Bmi1.IsSupported)
                flags |= HardwareSupport.Bmi1;
            if (Bmi2.IsSupported)
                flags |= HardwareSupport.Bmi2;
            if (Fma.IsSupported)
                flags |= HardwareSupport.Fma;
            if (Lzcnt.IsSupported)
                flags |= HardwareSupport.Lzcnt;
            if (Pclmulqdq.IsSupported)
                flags |= HardwareSupport.Pclmulqdq;
            if (Popcnt.IsSupported)
                flags |= HardwareSupport.Popcnt;
            if (Sse.IsSupported)
                flags |= HardwareSupport.Sse;
            if (Sse2.IsSupported)
                flags |= HardwareSupport.Sse2;
            if (Sse3.IsSupported)
                flags |= HardwareSupport.Sse3;
            if (Sse41.IsSupported)
                flags |= HardwareSupport.Sse41;
            if (Sse42.IsSupported)
                flags |= HardwareSupport.Sse42;
            if (Ssse3.IsSupported)
                flags |= HardwareSupport.Ssse3;
            return flags;
        }

        private static HardwareSupport GetArmFlags()
        {
            HardwareSupport flags = HardwareSupport.Arm;
            if (ArmBase.IsSupported)
                flags |= HardwareSupport.ArmBase;
            if (AdvSimd.IsSupported)
                flags |= HardwareSupport.AdvSimd;
            if (System.Runtime.Intrinsics.Arm.Aes.IsSupported)
                flags |= HardwareSupport.Aes;
            if (Crc32.IsSupported)
                flags |= HardwareSupport.Crc32;
            if (Dp.IsSupported)
                flags |= HardwareSupport.Dp;
            if (Rdm.IsSupported)
                flags |= HardwareSupport.Rdm;
            if (Sha1.IsSupported)
                flags |= HardwareSupport.Sha1;
            if (Sha256.IsSupported)
                flags |= HardwareSupport.Sha256;
            return flags;
        }
    }
}
