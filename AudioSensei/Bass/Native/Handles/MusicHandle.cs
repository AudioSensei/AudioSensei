using System;
using System.Runtime.InteropServices;

namespace AudioSensei.Bass.Native.Handles
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct MusicHandle : IEquatable<MusicHandle>
    {
        public static readonly MusicHandle Null = new(0);

        private readonly uint handle;
        private MusicHandle(uint handle) => this.handle = handle;

        public bool Equals(MusicHandle other)
        {
            return handle == other.handle;
        }

        public override bool Equals(object obj)
        {
            return obj is MusicHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)handle;
        }

        public static bool operator ==(MusicHandle left, MusicHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MusicHandle left, MusicHandle right)
        {
            return !left.Equals(right);
        }
    }
}
