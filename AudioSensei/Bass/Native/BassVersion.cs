using System.Runtime.InteropServices;

namespace AudioSensei.Bass.Native
{
    [StructLayout(LayoutKind.Explicit, Size = sizeof(uint))]
    internal readonly struct BassVersion
    {
        [FieldOffset(0)]
        public readonly uint version;

        [FieldOffset(sizeof(uint) / 4 * 3)]
        public readonly byte major;
        [FieldOffset(sizeof(uint) / 4 * 2)]
        public readonly byte minor;
        [FieldOffset(sizeof(uint) / 4 * 1)]
        public readonly byte patch;
        [FieldOffset(sizeof(uint) / 4 * 0)]
        public readonly byte revision;

        public override string ToString()
        {
            return $"{major}.{minor}.{patch}.{revision}";
        }
    }
}
