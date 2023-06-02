using System.Runtime.InteropServices;

namespace AudioSensei.Discord
{
    internal static unsafe class DiscordNative
    {
        private const string DiscordRpc = "discord-rpc";

        [DllImport(DiscordRpc, EntryPoint = "Discord_Initialize")]
        public static extern void Initialize([MarshalAs(UnmanagedType.LPUTF8Str)] string applicationId, DiscordEventHandlers* handlers, bool autoRegister, [MarshalAs(UnmanagedType.LPUTF8Str)] string optionalSteamId = null, int optionalPipeNumber = 0);

        [DllImport(DiscordRpc, EntryPoint = "Discord_Shutdown")]
        public static extern void Shutdown();

        [DllImport(DiscordRpc, EntryPoint = "Discord_RunCallbacks")]
        public static extern void RunCallbacks();

        [DllImport(DiscordRpc, EntryPoint = "Discord_UpdatePresence")]
        public static extern void UpdatePresence(DiscordRichPresenceData.DiscordRichPresenceDataStruct* presence);

        [DllImport(DiscordRpc, EntryPoint = "Discord_ClearPresence")]
        public static extern void ClearPresence();

        [DllImport(DiscordRpc, EntryPoint = "Discord_Respond")]
        public static extern void Respond([MarshalAs(UnmanagedType.LPUTF8Str)] string userId, DiscordReply reply);

        [DllImport(DiscordRpc, EntryPoint = "Discord_UpdateHandlers")]
        public static extern void UpdateHandlers(DiscordEventHandlers* handlers);
    }
}
