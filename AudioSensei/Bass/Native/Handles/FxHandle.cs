using System;
using System.Runtime.InteropServices;

namespace AudioSensei.Bass.Native.Handles
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct FxHandle : IEquatable<FxHandle>
    {
        public static readonly FxHandle Null = new(0);

        private readonly uint handle;
        private FxHandle(uint handle) => this.handle = handle;

        public bool Equals(FxHandle other)
        {
            return handle == other.handle;
        }

        public override bool Equals(object obj)
        {
            return obj is FxHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)handle;
        }

        public static bool operator ==(FxHandle left, FxHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FxHandle left, FxHandle right)
        {
            return !left.Equals(right);
        }
    }
}
