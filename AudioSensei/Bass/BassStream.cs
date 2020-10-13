using System;
using System.Threading;
using AudioSensei.Bass.Native;
using AudioSensei.Bass.Native.Handles;

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
                    case ChannelStatus.Stalled:
                        return AudioStreamStatus.Playing;
                    case ChannelStatus.Paused:
                    case ChannelStatus.PausedDevice:
                        return AudioStreamStatus.Paused;
                    default:
                        return AudioStreamStatus.Invalid;
                }
            }
        }

        private bool _disposed;

        public StreamHandle Handle { get; }
        protected readonly BassChannelInfo Info;
        private readonly SyncHandle _restartSync;
        private readonly object _freeLock = new object();

        internal BassStream(StreamHandle handle)
        {
            if (handle == StreamHandle.Null)
            {
                throw new InvalidOperationException();
            }

            if (BassNative.Singleton.GetChannelStatus(Handle) == ChannelStatus.Stopped && BassNative.GetLastErrorCode() != BassError.Ok)
            {
                throw new InvalidOperationException();
            }

            Handle = handle;
            Info = BassNative.Singleton.GetChannelInfo(Handle);

            BassNative.Singleton.PlayChannel(Handle);

            _restartSync = BassNative.Singleton.SetSync(handle, BassSync.DevFail, 0, (u, channel, data, user) => BassNative.Singleton.Restart(), IntPtr.Zero);
            BassNative.Singleton.SetSync(handle, BassSync.Free, 0, (u, channel, data, user) =>
            {
                if (Monitor.TryEnter(_freeLock))
                {
                    GC.SuppressFinalize(this);
                    _disposed = true;
                }
            }, IntPtr.Zero);
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
            var lockWasTaken = false;
            var temp = _freeLock;
            try
            {
                Monitor.Enter(temp, ref lockWasTaken);
                if (_disposed || (BassNative.Singleton.GetChannelStatus(Handle) == ChannelStatus.Stopped && BassNative.GetLastErrorCode() != BassError.Ok))
                {
                    return;
                }

                BassNative.Singleton.RemoveSync(Handle, _restartSync);
                BassNative.Singleton.StopChannel(Handle);
                BassNative.Singleton.FreeStream(Handle);

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
