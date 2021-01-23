using System;
using System.Runtime.InteropServices;

namespace AudioSensei.Bass.Native.Handles
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct PluginHandle : IEquatable<PluginHandle>
    {
        public static readonly PluginHandle Null = new(0);

        private readonly uint handle;
        private PluginHandle(uint handle) => this.handle = handle;

        public bool Equals(PluginHandle other)
        {
            return handle == other.handle;
        }

        public override bool Equals(object obj)
        {
            return obj is PluginHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)handle;
        }

        public static bool operator ==(PluginHandle left, PluginHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PluginHandle left, PluginHandle right)
        {
            return !left.Equals(right);
        }
    }
}
