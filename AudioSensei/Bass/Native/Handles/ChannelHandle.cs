using System;
using System.Runtime.InteropServices;

namespace AudioSensei.Bass.Native.Handles
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct ChannelHandle : IEquatable<ChannelHandle>
    {
        public static readonly ChannelHandle Null = new ChannelHandle(0);

        private readonly uint handle;
        private ChannelHandle(uint handle) => this.handle = handle;

        public bool Equals(ChannelHandle other)
        {
            return handle == other.handle;
        }

        public override bool Equals(object obj)
        {
            return obj is ChannelHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)handle;
        }

        public static bool operator ==(ChannelHandle left, ChannelHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ChannelHandle left, ChannelHandle right)
        {
            return !left.Equals(right);
        }
    }
}
