using System;
using System.Runtime.InteropServices;

namespace AudioSensei.Discord
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct DiscordEventHandlers
    {
        public IntPtr userData;
        public DiscordDelegates.Ready readyCallback;
        public DiscordDelegates.Disconnected disconnectedCallback;
        public DiscordDelegates.Errored errorCallback;
        public DiscordDelegates.JoinGame joinCallback;
        public DiscordDelegates.SpectateGame spectateCallback;
        public DiscordDelegates.JoinRequest requestCallback;
    }
}
