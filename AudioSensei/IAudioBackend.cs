using System;
using System.ComponentModel;

namespace AudioSensei
{
    public interface IAudioBackend : IDisposable, INotifyPropertyChanged
    {
        float Volume { get; set; }

        string TotalTimeFormatted { get; }
        string CurrentTimeFormatted { get; }
        TimeSpan TotalTime { get; }
        TimeSpan CurrentTime { get; }
        int Total { get; }

        bool IsInitialized { get; }
        bool IsPlaying { get; }
        bool IsPaused { get; }

        void PlayFile(string filePath);
        void PlayUrl(string url);
        void Resume();
        void Pause();
        void Stop();
    }
}
