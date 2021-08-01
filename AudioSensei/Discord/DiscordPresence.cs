using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Serilog;

namespace AudioSensei.Discord
{
    public unsafe class DiscordPresence : IDisposable
    {
        private readonly int _sleepTime;
        private volatile int _disposed;

        private static readonly object DisposeLock = new();

        private readonly DiscordEventHandlers* _handlers;
        private readonly DiscordRichPresenceData _presence;

        public DiscordPresence(string appId, int sleepTime = 150)
        {
            lock (DisposeLock)
            {
                _sleepTime = sleepTime;

                [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
                static void Ready(DiscordUserData* request, IntPtr _)
                {
                    if (request == null)
                        Log.Warning("Received null user info from discord!");
                    else
                        Log.Information($"Discord connected to {Marshal.PtrToStringUTF8(request->username)}#{Marshal.PtrToStringUTF8(request->discriminator)} with ID {Marshal.PtrToStringUTF8(request->userId)}");
                }
                [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
                static void Disconnected(int code, IntPtr message, IntPtr _) =>
                    Log.Information($"Discord disconnected with code {code} with message {Marshal.PtrToStringUTF8(message)}");
                [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
                static void Errored(int code, IntPtr message, IntPtr _) =>
                    Log.Information($"Discord errored with code {code} with message {Marshal.PtrToStringUTF8(message)}");

                _handlers = UnmanagedUtils.Alloc<DiscordEventHandlers>(1);
                *_handlers = new DiscordEventHandlers
                {
                    userData = IntPtr.Zero,
                    readyCallback = &Ready,
                    disconnectedCallback = &Disconnected,
                    errorCallback = &Errored,
                    joinCallback = null,
                    spectateCallback = null,
                    requestCallback = null
                };

                DiscordNative.Initialize(appId, _handlers, false);

                new Thread(RunCallbacks) { IsBackground = true, Priority = ThreadPriority.BelowNormal }.Start();

                _presence = new DiscordRichPresenceData
                {
                    StartTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                DiscordNative.UpdatePresence(_presence.Handle);
            }
        }

        public void UpdatePresence(string state, string details, long startTimestamp, long endTimestamp)
        {
            lock (DisposeLock)
            {
                _presence.State = state;
                _presence.Details = details;
                _presence.StartTimestamp = startTimestamp;
                _presence.EndTimestamp = endTimestamp;
                DiscordNative.UpdatePresence(_presence.Handle);
            }
        }

        private void RunCallbacks()
        {
            while (true)
            {
                lock (DisposeLock)
                {
                    if (_disposed != 0)
                        return;
                    DiscordNative.RunCallbacks();
                }
                Thread.Sleep(_sleepTime);
            }
        }

        private void Free()
        {
            lock (DisposeLock)
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                    return;
                DiscordNative.ClearPresence();
                DiscordNative.Shutdown();
                UnmanagedUtils.Free(_handlers);
                _presence.Dispose();
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
