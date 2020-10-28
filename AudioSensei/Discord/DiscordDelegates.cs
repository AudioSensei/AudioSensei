using System;
using System.Runtime.InteropServices;

namespace AudioSensei.Discord
{
    internal static class DiscordDelegates
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void Ready(DiscordUserData* request, IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Disconnected(int errorCode, IntPtr message, IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Errored(int errorCode, IntPtr message, IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void JoinGame(IntPtr joinSecret, IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void SpectateGame(IntPtr spectateSecret, IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void JoinRequest(DiscordUserData* request, IntPtr userData);
    }
}
