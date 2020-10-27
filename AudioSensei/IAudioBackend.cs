using System;
using System.ComponentModel;

namespace AudioSensei
{
    public interface IAudioBackend : IDisposable, INotifyPropertyChanged
    {
        float Volume { get; set; }
        
        IAudioStream Play(Uri uri);
    }
}
