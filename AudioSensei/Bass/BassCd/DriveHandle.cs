using System;
using System.Runtime.InteropServices;

namespace AudioSensei.Bass.BassCd
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct DriveHandle : IEquatable<DriveHandle>
    {
        private readonly uint handle;
        public DriveHandle(uint index) => handle = index;

        public bool Equals(DriveHandle other)
        {
            return handle == other.handle;
        }

        public override bool Equals(object obj)
        {
            return obj is DriveHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)handle;
        }

        public static bool operator ==(DriveHandle left, DriveHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DriveHandle left, DriveHandle right)
        {
            return !left.Equals(right);
        }
    }
}
