using System;
using System.Runtime.InteropServices;

namespace AudioSensei.Bass
{
	[StructLayout(LayoutKind.Sequential)]
	internal readonly struct BassHandle : IEquatable<BassHandle>
	{
		public static readonly BassHandle Null = new BassHandle(0);

		private readonly uint handle;
		private BassHandle(uint handle) => this.handle = handle;

		public bool Equals(BassHandle other)
		{
			return handle == other.handle;
		}

		public override bool Equals(object obj)
		{
			return obj is BassHandle other && Equals(other);
		}

		public override int GetHashCode()
		{
			return (int)handle;
		}

		public static bool operator ==(BassHandle left, BassHandle right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(BassHandle left, BassHandle right)
		{
			return !left.Equals(right);
		}
	}
}
