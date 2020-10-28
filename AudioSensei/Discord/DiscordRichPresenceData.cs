using System.Runtime.InteropServices;

namespace AudioSensei.Discord
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct DiscordRichPresenceData
    {
        [MarshalAs(UnmanagedType.LPUTF8Str)]
        public string state;   /* max 128 bytes */
        [MarshalAs(UnmanagedType.LPUTF8Str)]
        public string details; /* max 128 bytes */
        public long startTimestamp;
        public long endTimestamp;
        [MarshalAs(UnmanagedType.LPUTF8Str)]
        public string largeImageKey;  /* max 32 bytes */
        [MarshalAs(UnmanagedType.LPUTF8Str)]
        public string largeImageText; /* max 128 bytes */
        [MarshalAs(UnmanagedType.LPUTF8Str)]
        public string smallImageKey;  /* max 32 bytes */
        [MarshalAs(UnmanagedType.LPUTF8Str)]
        public string smallImageText; /* max 128 bytes */
        [MarshalAs(UnmanagedType.LPUTF8Str)]
        public string partyId;        /* max 128 bytes */
        public int partySize;
        public int partyMax;
        public DiscordPartyPrivacy partyPrivacy;
        [MarshalAs(UnmanagedType.LPUTF8Str)]
        public string matchSecret;    /* max 128 bytes */
        [MarshalAs(UnmanagedType.LPUTF8Str)]
        public string joinSecret;     /* max 128 bytes */
        [MarshalAs(UnmanagedType.LPUTF8Str)]
        public string spectateSecret; /* max 128 bytes */
        public sbyte instance;
    }
}
