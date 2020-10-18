using System;
using System.ComponentModel;

namespace AudioSensei
{
    public interface IAudioBackend : IDisposable, INotifyPropertyChanged
    {
        IAudioStream Play(Uri uri);
    }
}
