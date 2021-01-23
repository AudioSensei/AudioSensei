using System;
using System.Runtime.InteropServices;

namespace AudioSensei.Bass.Native.Handles
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct RecordHandle : IEquatable<RecordHandle>
    {
        public static readonly RecordHandle Null = new(0);

        private readonly uint handle;
        private RecordHandle(uint handle) => this.handle = handle;

        public bool Equals(RecordHandle other)
        {
            return handle == other.handle;
        }

        public override bool Equals(object obj)
        {
            return obj is RecordHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)handle;
        }

        public static bool operator ==(RecordHandle left, RecordHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RecordHandle left, RecordHandle right)
        {
            return !left.Equals(right);
        }
    }
}
