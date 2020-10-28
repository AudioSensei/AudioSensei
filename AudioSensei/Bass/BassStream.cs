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
                {
                    return AudioStreamStatus.Invalid;
                }

                switch (BassNative.Singleton.GetChannelStatus(Handle))
                {
                    case ChannelStatus.Playing:
                        return AudioStreamStatus.Playing;
                    case ChannelStatus.Stalled:
                    case ChannelStatus.Paused:
                    case ChannelStatus.PausedDevice:
                        return AudioStreamStatus.Paused;
                    default:
                        return AudioStreamStatus.Invalid;
                }
            }
        }

        private volatile bool _disposed;
        private volatile bool _streamFreed;

        public StreamHandle Handle { get; }
        protected readonly BassChannelInfo Info;
        private readonly SyncHandle _restartSync;
        private readonly object _freeLock = new object();

        private readonly BassNative.SyncProc _failProc;
        private readonly BassNative.SyncProc _freeProc;

        internal BassStream(StreamHandle handle)
        {
            if (!BassNative.Singleton.IsHandleValid(handle))
            {
                throw new InvalidOperationException();
            }

            Handle = handle;
            Info = BassNative.Singleton.GetChannelInfo(Handle);
            if (Info.plugin != PluginHandle.Null && BassNative.Singleton.Plugins.TryGetValue(Info.plugin, out var value))
            {
                Log.Information($"Using bass plugin {value.manifest.Name} version {value.info.version} to play {Info.FileName}");
            }

            BassNative.Singleton.PlayChannel(Handle);

            _failProc = (u, channel, data, user) => BassNative.Singleton.Restart();
            _restartSync = BassNative.Singleton.SetSync(handle, BassSync.DevFail, 0, _failProc, IntPtr.Zero);

            _freeProc = (u, channel, data, user) =>
            {
                _streamFreed = true;
                if (Monitor.TryEnter(_freeLock))
                {
                    GC.SuppressFinalize(this);
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
            var temp = _freeLock;
            try
            {
                Monitor.Enter(temp, ref lockWasTaken);
                if (_streamFreed || _disposed || !BassNative.Singleton.IsHandleValid(Handle))
                {
                    return;
                }

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
                    Monitor.Exit(temp);
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
