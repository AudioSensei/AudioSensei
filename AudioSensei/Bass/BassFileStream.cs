using System.IO;
using AudioSensei.Bass.Native;
using JetBrains.Annotations;

namespace AudioSensei.Bass
{
    internal class BassFileStream : BassStream, IFileStream
    {
        public string FilePath { get; }

        internal BassFileStream([NotNull] string path) : base(BassNative.Singleton.CreateStreamFromFile(path))
        {
            var bassPath = Path.GetFullPath(Info.FileName);
            FilePath = File.Exists(bassPath) ? bassPath : Path.GetFullPath(path);
        }
    }
}
