using System;
using System.Runtime.InteropServices;

namespace AudioSensei.Discord
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct DiscordEventHandlers
    {
        public IntPtr userData;
        public delegate* unmanaged[Cdecl]<DiscordUserData*, IntPtr, void> readyCallback;
        public delegate* unmanaged[Cdecl]<int, IntPtr, IntPtr, void> disconnectedCallback;
        public delegate* unmanaged[Cdecl]<int, IntPtr, IntPtr, void> errorCallback;
        public delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> joinCallback;
        public delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> spectateCallback;
        public delegate* unmanaged[Cdecl]<DiscordUserData*, IntPtr, void> requestCallback;
    }
}
