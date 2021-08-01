using System;
using System.Runtime.InteropServices;
using System.Threading;
using AudioSensei.Bass.Native;
using AudioSensei.Bass.Native.Handles;
using Serilog;

namespace AudioSensei.Bass
{
    internal abstract unsafe class BassStream : IAudioStream
    {
        public TimeSpan TotalTime => TimeSpan.FromSeconds(BassNative.Singleton.ConvertBytesToSeconds(Handle, BassNative.Singleton.GetChannelLength(Handle, LengthMode.Bytes)));
        public TimeSpan CurrentTime => TimeSpan.FromSeconds(BassNative.Singleton.ConvertBytesToSeconds(Handle, BassNative.Singleton.GetChannelPosition(Handle, LengthMode.Bytes)));

        public AudioStreamStatus Status
        {
            get
            {
                if (_disposed != 0)
                    return AudioStreamStatus.Invalid;

                return BassNative.Singleton.GetChannelStatus(Handle) switch
                {
                    ChannelStatus.Playing => AudioStreamStatus.Playing,
                    ChannelStatus.Stalled => AudioStreamStatus.Paused,
                    ChannelStatus.Paused => AudioStreamStatus.Paused,
                    ChannelStatus.PausedDevice => AudioStreamStatus.Paused,
                    _ => AudioStreamStatus.Invalid
                };
            }
        }

        private readonly object _freeLock = new();
        private readonly SyncHandle _restartSync;
        private volatile int _disposed;
        private GCHandle _thisHandle;

        public StreamHandle Handle { get; }
        protected readonly BassChannelInfo Info;

        internal BassStream(StreamHandle handle)
        {
            if (!BassNative.Singleton.IsHandleValid(handle))
                throw new InvalidOperationException();

            Handle = handle;
            Info = BassNative.Singleton.GetChannelInfo(Handle);
            if (Info.plugin != PluginHandle.Null && BassNative.Singleton.Plugins.TryGetValue(Info.plugin, out var value))
                Log.Information($"Using bass plugin {value.manifest.Name} version {value.info.version} to play {Info.FileName}");

            BassNative.Singleton.PlayChannel(Handle);

            [UnmanagedCallersOnly]
            static void FailProc(uint u, uint u1, uint u2, IntPtr intPtr) =>
                BassNative.Singleton.Restart();

            _restartSync = BassNative.Singleton.SetSync(handle, BassSync.DevFail, 0, &FailProc, IntPtr.Zero);

            [UnmanagedCallersOnly]
            static void FreeProc(uint u, uint u1, uint u2, IntPtr intPtr)
            {
                BassStream stream = (BassStream)GCHandle.FromIntPtr(intPtr).Target;
                if (Interlocked.Exchange(ref stream!._disposed, 1) != 0)
                    return;
                stream._thisHandle.Free();
#pragma warning disable CA1816
                GC.SuppressFinalize(stream);
#pragma warning restore CA1816
            }

            _thisHandle = GCHandle.Alloc(this);

            BassNative.Singleton.SetSync(handle, BassSync.Free, 0, &FreeProc, GCHandle.ToIntPtr(_thisHandle));
        }

        public void Resume()
        {
            if (BassNative.Singleton.GetChannelStatus(Handle) == ChannelStatus.PausedDevice)
            {
                BassNative.Singleton.Restart();
            }
            BassNative.Singleton.PlayChannel(Handle);
        }

        public void Pause()
        {
            BassNative.Singleton.PauseChannel(Handle);
        }

        private void Free()
        {
            lock (_freeLock)
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                    return;
                if (!BassNative.Singleton.IsHandleValid(Handle))
                    return;
                BassNative.Singleton.RemoveSync(Handle, _restartSync);
                BassNative.Singleton.StopChannel(Handle);
                BassNative.Singleton.FreeStream(Handle);
                _thisHandle.Free();
            }
        }

        public void Dispose()
        {
            Free();
            GC.SuppressFinalize(this);
        }

        ~BassStream()
        {
            Free();
        }
    }
}
