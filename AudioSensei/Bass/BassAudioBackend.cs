using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using AudioSensei.Bass.Native;
using AudioSensei.Configuration;
using JetBrains.Annotations;

namespace AudioSensei.Bass
{
    public class BassAudioBackend : IAudioBackend, IVolumeControl
    {
        private IAudioStream _stream;
        private readonly BassNative _bassNative;

        public event PropertyChangedEventHandler PropertyChanged;

        public float Volume
        {
            get
            {
                return _bassNative.GetConfig(BassConfig.GlobalVolumeStream) / 100f;
            }
            set
            {
                uint volume = Math.Min((uint)(value * 100), 10000);
                _bassNative.SetConfig(BassConfig.GlobalVolumeMusic, volume);
                _bassNative.SetConfig(BassConfig.GlobalVolumeSample, volume);
                _bassNative.SetConfig(BassConfig.GlobalVolumeStream, volume);
                OnPropertyChanged(nameof(Volume));
            }
        }

        public BassAudioBackend([NotNull] BassConfiguration bassConfiguration, IntPtr windowHandle = default)
        {
            _bassNative = new BassNative(bassConfiguration, windowHandle: windowHandle);
        }

        [CanBeNull, Pure]
        public IAudioStream Play([NotNull] Uri uri)
        {
            if (uri.IsFile)
            {
                var filePath = Path.GetFullPath(uri.LocalPath);

                if (!File.Exists(filePath))
                {
                    return null;
                }

                _stream = new BassFileStream(filePath);
            }
            else
            {
                _stream = new BassWebStream(uri);
            }

            return _stream;
        }

        private void Free()
        {
            _stream?.Dispose();
            _bassNative.Dispose();
        }

        ~BassAudioBackend()
        {
            Free();
        }

        public void Dispose()
        {
            Free();
            GC.SuppressFinalize(this);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName, CanBeNull] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
