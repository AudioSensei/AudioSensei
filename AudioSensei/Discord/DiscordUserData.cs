using System;
using System.Runtime.InteropServices;

namespace AudioSensei.Discord
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct DiscordUserData
    {
        public IntPtr userId;
        public IntPtr username;
        public IntPtr discriminator;
        public IntPtr avatar;
    }
}
