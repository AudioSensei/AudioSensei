namespace AudioSensei
{
    public interface IFileStream : IAudioStream
    {
        string FilePath { get; }
    }
}
