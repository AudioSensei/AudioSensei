using System;
using System.Runtime.InteropServices;

namespace AudioSensei.Bass.Native.Handles
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct StreamHandle : IEquatable<StreamHandle>
    {
        public static readonly StreamHandle Null = new StreamHandle(0);

        private readonly uint handle;
        private StreamHandle(uint handle) => this.handle = handle;

        public bool Equals(StreamHandle other)
        {
            return handle == other.handle;
        }

        public override bool Equals(object obj)
        {
            return obj is StreamHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)handle;
        }

        public static bool operator ==(StreamHandle left, StreamHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StreamHandle left, StreamHandle right)
        {
            return !left.Equals(right);
        }
    }
}
