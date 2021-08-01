using System;
using System.Threading;
using AudioSensei.Bass.Native;
using AudioSensei.Bass.Native.Handles;
using Serilog;

namespace AudioSensei.Bass
{
    internal abstract class BassStream : IAudioStream
    {
        public TimeSpan TotalTime => TimeSpan.FromSeconds(BassNative.Singleton.ConvertBytesToSeconds(Handle, BassNative.Singleton.GetChannelLength(Handle, LengthMode.Bytes)));
        public TimeSpan CurrentTime => TimeSpan.FromSeconds(BassNative.Singleton.ConvertBytesToSeconds(Handle, BassNative.Singleton.GetChannelPosition(Handle, LengthMode.Bytes)));

        public AudioStreamStatus Status
        {
            get
            {
                if (_disposed)
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

        private volatile bool _disposed;
        private volatile bool _streamFreed;

        public StreamHandle Handle { get; }
        protected readonly BassChannelInfo Info;
        private readonly SyncHandle _restartSync;
        private readonly object _freeLock = new();

        // ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
        private readonly BassNative.SyncProc _failProc;
        private readonly BassNative.SyncProc _freeProc;
        // ReSharper restore PrivateFieldCanBeConvertedToLocalVariable

        internal BassStream(StreamHandle handle)
        {
            if (!BassNative.Singleton.IsHandleValid(handle))
                throw new InvalidOperationException();

            Handle = handle;
            Info = BassNative.Singleton.GetChannelInfo(Handle);
            if (Info.plugin != PluginHandle.Null && BassNative.Singleton.Plugins.TryGetValue(Info.plugin, out var value))
                Log.Information($"Using bass plugin {value.manifest.Name} version {value.info.version} to play {Info.FileName}");

            BassNative.Singleton.PlayChannel(Handle);

            _failProc = (_, _, _, _) => BassNative.Singleton.Restart();
            _restartSync = BassNative.Singleton.SetSync(handle, BassSync.DevFail, 0, _failProc, IntPtr.Zero);

            _freeProc = (_, _, _, _) =>
            {
                _streamFreed = true;
                if (Monitor.TryEnter(_freeLock))
                {
#pragma warning disable CA1816
                    GC.SuppressFinalize(this);
#pragma warning restore CA1816
                    _disposed = true;
                    Monitor.Exit(_freeLock);
                }
            };
            BassNative.Singleton.SetSync(handle, BassSync.Free, 0, _freeProc, IntPtr.Zero);
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

        private void FreeStream()
        {
            if (Handle == StreamHandle.Null)
                return;
            var lockWasTaken = false;
            try
            {
                Monitor.Enter(_freeLock, ref lockWasTaken);
                if (_streamFreed || _disposed || !BassNative.Singleton.IsHandleValid(Handle))
                    return;

                BassNative.Singleton.RemoveSync(Handle, _restartSync);
                BassNative.Singleton.StopChannel(Handle);
                if (!_streamFreed)
                {
                    BassNative.Singleton.FreeStream(Handle);
                    _streamFreed = true;
                }

                _disposed = true;
            }
            finally
            {
                if (lockWasTaken)
                {
                    Monitor.Exit(_freeLock);
                }
            }
        }

        public void Dispose()
        {
            FreeStream();
            GC.SuppressFinalize(this);
        }

        ~BassStream()
        {
            FreeStream();
        }
    }
}
