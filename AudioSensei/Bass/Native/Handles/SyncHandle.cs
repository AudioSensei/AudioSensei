using System;
using System.Runtime.InteropServices;

namespace AudioSensei.Bass.Native.Handles
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct SyncHandle : IEquatable<SyncHandle>
    {
        public static readonly SyncHandle Null = new SyncHandle(0);

        private readonly uint handle;
        private SyncHandle(uint handle) => this.handle = handle;

        public bool Equals(SyncHandle other)
        {
            return handle == other.handle;
        }

        public override bool Equals(object obj)
        {
            return obj is SyncHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)handle;
        }

        public static bool operator ==(SyncHandle left, SyncHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SyncHandle left, SyncHandle right)
        {
            return !left.Equals(right);
        }
    }
}
