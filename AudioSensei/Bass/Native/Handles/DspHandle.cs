using System;
using System.Runtime.InteropServices;

namespace AudioSensei.Bass.Native.Handles
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct DspHandle : IEquatable<DspHandle>
    {
        public static readonly DspHandle Null = new(0);

        private readonly uint handle;
        private DspHandle(uint handle) => this.handle = handle;

        public bool Equals(DspHandle other)
        {
            return handle == other.handle;
        }

        public override bool Equals(object obj)
        {
            return obj is DspHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)handle;
        }

        public static bool operator ==(DspHandle left, DspHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DspHandle left, DspHandle right)
        {
            return !left.Equals(right);
        }
    }
}
