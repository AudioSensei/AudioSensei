using System;

namespace AudioSensei
{
    public interface IAudioStream : IDisposable
    {
        TimeSpan TotalTime { get; }
        TimeSpan CurrentTime { get; }

        AudioStreamStatus Status { get; }

        void Resume();
        void Pause();
    }
}
