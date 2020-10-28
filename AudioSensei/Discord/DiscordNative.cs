using System.Runtime.InteropServices;

namespace AudioSensei.Discord
{
    internal static class DiscordNative
    {
        private const string DiscordRpc = "discord-rpc";

        [DllImport(DiscordRpc)]
        public static extern void Discord_Initialize([MarshalAs(UnmanagedType.LPUTF8Str)] string applicationId, ref DiscordEventHandlers handlers, bool autoRegister, [MarshalAs(UnmanagedType.LPUTF8Str)] string optionalSteamId = null, int optionalPipeNumber = 0);

        [DllImport(DiscordRpc)]
        public static extern void Discord_Shutdown();

        [DllImport(DiscordRpc)]
        public static extern void Discord_RunCallbacks();

        [DllImport(DiscordRpc)]
        public static extern void Discord_UpdatePresence(ref DiscordRichPresenceData presence);

        [DllImport(DiscordRpc)]
        public static extern void Discord_ClearPresence();

        [DllImport(DiscordRpc)]
        public static extern void Discord_Respond([MarshalAs(UnmanagedType.LPUTF8Str)] string userId, DiscordReply reply);

        [DllImport(DiscordRpc)]
        public static extern void Discord_UpdateHandlers(ref DiscordEventHandlers handlers);
    }
}
