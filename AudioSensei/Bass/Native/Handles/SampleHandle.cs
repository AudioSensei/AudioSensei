using System;
using System.Runtime.InteropServices;

namespace AudioSensei.Bass.Native.Handles
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct SampleHandle : IEquatable<SampleHandle>
    {
        public static readonly SampleHandle Null = new(0);

        private readonly uint handle;
        private SampleHandle(uint handle) => this.handle = handle;

        public bool Equals(SampleHandle other)
        {
            return handle == other.handle;
        }

        public override bool Equals(object obj)
        {
            return obj is SampleHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)handle;
        }

        public static bool operator ==(SampleHandle left, SampleHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SampleHandle left, SampleHandle right)
        {
            return !left.Equals(right);
        }
    }
}
