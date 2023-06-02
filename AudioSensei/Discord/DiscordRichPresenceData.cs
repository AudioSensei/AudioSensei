using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace AudioSensei.Discord
{
    internal sealed unsafe class DiscordRichPresenceData : IDisposable
    {
        public readonly DiscordRichPresenceDataStruct* Handle;
        private static readonly UTF8Encoding Encoding = new(false, true);
        private volatile int _disposed;

        public string State
        {
            get => Marshal.PtrToStringUTF8(new IntPtr(Handle->state));
            set => GetBytes(ref Handle->state, DiscordRichPresenceDataStruct.StateSize, value);
        }
        public string Details
        {
            get => Marshal.PtrToStringUTF8(new IntPtr(Handle->details));
            set => GetBytes(ref Handle->details, DiscordRichPresenceDataStruct.DetailsSize, value);
        }
        public ref long StartTimestamp => ref Handle->startTimestamp;
        public ref long EndTimestamp => ref Handle->endTimestamp;
        public string LargeImageKey
        {
            get => Marshal.PtrToStringUTF8(new IntPtr(Handle->largeImageKey));
            set => GetBytes(ref Handle->largeImageKey, DiscordRichPresenceDataStruct.LargeImageKeySize, value);
        }
        public string LargeImageText
        {
            get => Marshal.PtrToStringUTF8(new IntPtr(Handle->largeImageText));
            set => GetBytes(ref Handle->largeImageText, DiscordRichPresenceDataStruct.LargeImageTextSize, value);
        }
        public string SmallImageKey
        {
            get => Marshal.PtrToStringUTF8(new IntPtr(Handle->smallImageKey));
            set => GetBytes(ref Handle->smallImageKey, DiscordRichPresenceDataStruct.SmallImageKeySize, value);
        }
        public string SmallImageText
        {
            get => Marshal.PtrToStringUTF8(new IntPtr(Handle->smallImageText));
            set => GetBytes(ref Handle->smallImageText, DiscordRichPresenceDataStruct.SmallImageTextSize, value);
        }
        public string PartyId
        {
            get => Marshal.PtrToStringUTF8(new IntPtr(Handle->partyId));
            set => GetBytes(ref Handle->partyId, DiscordRichPresenceDataStruct.PartyIdSize, value);
        }
        public ref int PartySize => ref Handle->partySize;
        public ref int PartyMax => ref Handle->partyMax;
        public ref DiscordPartyPrivacy PartyPrivacy => ref Handle->partyPrivacy;
        public string MatchSecret
        {
            get => Marshal.PtrToStringUTF8(new IntPtr(Handle->matchSecret));
            set => GetBytes(ref Handle->matchSecret, DiscordRichPresenceDataStruct.MatchSecretSize, value);
        }
        public string JoinSecret
        {
            get => Marshal.PtrToStringUTF8(new IntPtr(Handle->joinSecret));
            set => GetBytes(ref Handle->joinSecret, DiscordRichPresenceDataStruct.JoinSecretSize, value);
        }
        public string SpectateSecret
        {
            get => Marshal.PtrToStringUTF8(new IntPtr(Handle->spectateSecret));
            set => GetBytes(ref Handle->spectateSecret, DiscordRichPresenceDataStruct.SpectateSecretSize, value);
        }
        public ref sbyte Instance => ref Handle->instance;

        public DiscordRichPresenceData()
        {
            Handle = UnmanagedUtils.Alloc<DiscordRichPresenceDataStruct>(1);
            *Handle = new DiscordRichPresenceDataStruct();
        }

        private static void GetBytes(ref byte* ptr, int maxSize, string value)
        {
            if (value == null)
            {
                UnmanagedUtils.Free(ptr);
                ptr = null;
                return;
            }
            if (ptr == null)
                ptr = UnmanagedUtils.Alloc<byte>(maxSize);
            ptr[Encoding.GetBytes(value, new Span<byte>(ptr, maxSize - 1))] = 0;
        }

        private void Free()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;
            UnmanagedUtils.Free(Handle->state);
            UnmanagedUtils.Free(Handle->details);
            UnmanagedUtils.Free(Handle->largeImageKey);
            UnmanagedUtils.Free(Handle->largeImageText);
            UnmanagedUtils.Free(Handle->smallImageKey);
            UnmanagedUtils.Free(Handle->smallImageText);
            UnmanagedUtils.Free(Handle->partyId);
            UnmanagedUtils.Free(Handle->matchSecret);
            UnmanagedUtils.Free(Handle->joinSecret);
            UnmanagedUtils.Free(Handle->spectateSecret);
            UnmanagedUtils.Free(Handle);
        }

        public void Dispose()
        {
            Free();
            GC.SuppressFinalize(this);
        }

        ~DiscordRichPresenceData()
        {
            Free();
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DiscordRichPresenceDataStruct
        {
            public byte* state;
            public const int StateSize = 128;
            public byte* details;
            public const int DetailsSize = 128;
            public long startTimestamp;
            public long endTimestamp;
            public byte* largeImageKey;
            public const int LargeImageKeySize = 32;
            public byte* largeImageText;
            public const int LargeImageTextSize = 128;
            public byte* smallImageKey;
            public const int SmallImageKeySize = 32;
            public byte* smallImageText;
            public const int SmallImageTextSize = 128;
            public byte* partyId;
            public const int PartyIdSize = 128;
            public int partySize;
            public int partyMax;
            public DiscordPartyPrivacy partyPrivacy;
            public byte* matchSecret;
            public const int MatchSecretSize = 128;
            public byte* joinSecret;
            public const int JoinSecretSize = 128;
            public byte* spectateSecret;
            public const int SpectateSecretSize = 128;
            public sbyte instance;
        }
    }
}
