using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using JetBrains.Annotations;

namespace AudioSensei.Bass
{
    public class BassAudioBackend : IAudioBackend
    {
        public float Volume
        {
            get => IsInitialized ? handle.GetChannelAttribute(ChannelAttribute.VolumeLevel) * 100f : 100f;
            set
            {
                if (IsInitialized)
                {
                    handle.SetChannelAttribute(ChannelAttribute.VolumeLevel, value / 100f);
                    this.OnPropertyChanged(nameof(Volume));
                }
            }
        }

        public string TotalTimeFormatted => TotalTime.Hours == 0 && CurrentTime.Hours == 0
            ? $@"{TotalTime:mm\:ss}"
            : $@"{TotalTime:hh\:mm\:ss}";
        public string CurrentTimeFormatted => TotalTime.Hours == 0 && CurrentTime.Hours == 0
            ? $@"{CurrentTime:mm\:ss}"
            : $@"{CurrentTime:hh\:mm\:ss}";
        public TimeSpan TotalTime
        {
            get
            {
                var seconds = IsInitialized ? handle.ConvertBytesToSeconds(handle.GetChannelLength(LengthMode.Bytes)) : 0.0;

                return TimeSpan.FromSeconds(seconds);
            }
        }
        public TimeSpan CurrentTime
        {
            get
            {
                var seconds = IsInitialized ? handle.ConvertBytesToSeconds(handle.GetChannelPosition(LengthMode.Bytes)) : 0.0;
                
                return TimeSpan.FromSeconds(seconds);
            }
        }
        public int Total => (int)(CurrentTime / TotalTime * 100);

        public bool IsInitialized => handle != BassHandle.Null;
        public bool IsPlaying => IsInitialized && handle.GetChannelStatus() == ChannelStatus.Playing;
        public bool IsPaused => IsInitialized && handle.GetChannelStatus() == ChannelStatus.Paused;

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(1000.0)
        };

        private BassHandle handle = BassHandle.Null;

        public BassAudioBackend(IntPtr windowHandle = default)
        {
            BassNative.Initialize(windowHandle: windowHandle);

            timer.Tick += Tick;
        }

        ~BassAudioBackend()
        {
            BassNative.Free();
        }

        public void PlayFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            if (IsInitialized)
            {
                handle.StopChannel();
            }
            
            handle = BassNative.CreateStreamFromFile(filePath);
            handle.PlayChannel();
            timer.Start();
        }

        public void PlayUrl(string url)
        {
            if (IsInitialized)
            {
                handle.StopChannel();
            }

            handle = BassNative.CreateStreamFromUrl(url);
            handle.PlayChannel();
            timer.Start();
        }

        public void Resume()
        {
            if (IsInitialized)
            {
                handle.PlayChannel();
                timer.Start();
            }
        }

        public void Pause()
        {
            if (IsInitialized)
            {
                handle.PauseChannel();
                timer.Stop();
            }
        }

        public void Stop()
        {
            if (IsInitialized)
            {
                handle.StopChannel();
                timer.Stop();
            }
        }
        
        public void Dispose()
        {
            BassNative.Free();
            GC.SuppressFinalize(this);
        }
        
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private void Tick(object sender, EventArgs args)
        {
            this.OnPropertyChanged(nameof(TotalTimeFormatted));
            this.OnPropertyChanged(nameof(CurrentTimeFormatted));
            this.OnPropertyChanged(nameof(TotalTime));
            this.OnPropertyChanged(nameof(CurrentTime));
            this.OnPropertyChanged(nameof(Total));
        }
    }
}
