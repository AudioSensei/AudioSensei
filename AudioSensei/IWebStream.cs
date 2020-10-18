using System;

namespace AudioSensei
{
    public interface IWebStream : IAudioStream
    {
        Uri Uri { get; }
    }
}
