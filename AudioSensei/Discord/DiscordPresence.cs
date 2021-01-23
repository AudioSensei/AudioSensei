using System;
using System.Runtime.InteropServices;
using System.Threading;
using Serilog;

namespace AudioSensei.Discord
{
    public class DiscordPresence : IDisposable
    {
        private readonly int _sleepTime;
        private volatile bool _disposed;
        private readonly Thread _updateThread;

        private static readonly object DisposeLock = new();
        private static readonly object PresenceLock = new();

        // ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
        private readonly DiscordEventHandlers _handlers;
        private DiscordRichPresenceData _presence;

        private readonly DiscordDelegates.Ready _readyCallback;
        private readonly DiscordDelegates.Disconnected _disconnectedCallback;
        private readonly DiscordDelegates.Errored _errorCallback;
        // ReSharper restore PrivateFieldCanBeConvertedToLocalVariable

        public unsafe DiscordPresence(string appId, int sleepTime = 15)
        {
            lock (PresenceLock)
            {
                _sleepTime = sleepTime;
                _readyCallback = (request, _) =>
                {
                    if (request == null)
                        Log.Warning("Received null user info from discord!");
                    else
                        Log.Information($"Discord connected to {Marshal.PtrToStringUTF8(request->username)}#{Marshal.PtrToStringUTF8(request->discriminator)} with ID {Marshal.PtrToStringUTF8(request->userId)}");
                };
                _disconnectedCallback = (code, message, _) => Log.Information($"Discord disconnected with code {code} with message {Marshal.PtrToStringUTF8(message)}");
                _errorCallback = (code, message, _) => Log.Information($"Discord errored with code {code} with message {Marshal.PtrToStringUTF8(message)}");
                _handlers = new DiscordEventHandlers
                {
                    userData = IntPtr.Zero,
                    readyCallback = _readyCallback,
                    disconnectedCallback = _disconnectedCallback,
                    errorCallback = _errorCallback,
                    joinCallback = null,
                    spectateCallback = null,
                    requestCallback = null
                };
                lock (DisposeLock)
                {
                    DiscordNative.Discord_Initialize(appId, ref _handlers, false);

                    _updateThread = new Thread(RunCallbacks) { IsBackground = true, Priority = ThreadPriority.BelowNormal };
                    _updateThread.Start();
                }

                _presence = new DiscordRichPresenceData
                {
                    state = null,
                    details = null,
                    startTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    endTimestamp = 0,
                    largeImageKey = null,
                    largeImageText = null,
                    smallImageKey = null,
                    smallImageText = null,
                    partyId = null,
                    partySize = 0,
                    partyMax = 0,
                    partyPrivacy = DiscordPartyPrivacy.Private,
                    matchSecret = null,
                    joinSecret = null,
                    spectateSecret = null,
                    instance = 0
                };
                DiscordNative.Discord_UpdatePresence(ref _presence);
            }
        }

        public void UpdatePresence(string state, string details, long startTimestamp, long endTimestamp)
        {
            lock (PresenceLock)
            {
                _presence.state = state;
                _presence.details = details;
                _presence.startTimestamp = startTimestamp;
                _presence.endTimestamp = endTimestamp;
                DiscordNative.Discord_UpdatePresence(ref _presence);
            }
        }

        private void RunCallbacks()
        {
            while (true)
            {
                lock (DisposeLock)
                {
                    if (_disposed)
                    {
                        return;
                    }
                    DiscordNative.Discord_RunCallbacks();
                }
                Thread.Sleep(_sleepTime);
            }
        }

        private void Free()
        {
            lock (PresenceLock)
            {
                lock (DisposeLock)
                {
                    if (_disposed)
                    {
                        return;
                    }
                    DiscordNative.Discord_ClearPresence();
                    DiscordNative.Discord_Shutdown();
                    _disposed = true;
                }

                _updateThread?.JoinOrTerminate(_sleepTime * 10);
            }
        }

        public void Dispose()
        {
            Free();
            GC.SuppressFinalize(this);
        }

        ~DiscordPresence()
        {
            Free();
        }
    }
}
