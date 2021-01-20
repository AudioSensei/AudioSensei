using System;
using System.Collections.Generic;
using AudioSensei.Bass;
using AudioSensei.Bass.Native.Handles;

namespace AudioSensei
{
    public interface IAudioStream : IDisposable
    {
        TimeSpan TotalTime { get; }
        TimeSpan CurrentTime { get; }

        AudioStreamStatus Status { get; }

        void Resume();
        void Pause();
        FxHandle AddEffect(BassModEffect effect, int priority);
        void SetEffectParameters(FxHandle effect, IEffect parameters);
    }
}
