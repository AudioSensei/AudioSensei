using AudioSensei.Bass;
using AudioSensei.Bass.Native;
using Serilog;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;

namespace AudioSensei.Tests
{
    public class BassTests
    {
        public BassTests(ITestOutputHelper output)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(output)
                .CreateLogger()
                .ForContext<BassTests>();
        }

        [Fact]
        public void TestInitialization()
        {
            // ReSharper disable once UnusedVariable
            using (var bass = new BassNative())
            {
            }
        }

        [Theory]
        [InlineData("test.wav")]
        public void TestPlayback(string path)
        {
            // ReSharper disable once UnusedVariable
            using (var bass = new BassNative())
            {
                using (var stream = new BassFileStream(path))
                {
                    Assert.NotNull(stream);
                    Assert.NotEqual(Bass.Native.Handles.StreamHandle.Null, stream.Handle);

                    stream.Pause();
                    Assert.Equal(AudioStreamStatus.Paused, stream.Status);

                    stream.Resume();
                    Assert.Equal(AudioStreamStatus.Playing, stream.Status);

                    stream.Dispose();
                    Assert.Equal(AudioStreamStatus.Invalid, stream.Status);
                }
            }
        }

        [Theory]
        [InlineData("test.wav")]
        public void TestChannelAttributes(string path)
        {
            using (var bass = new BassNative())
            {
                using (var stream = new BassFileStream(path))
                {
                    Assert.NotNull(stream);
                    Assert.NotEqual(Bass.Native.Handles.StreamHandle.Null, stream.Handle);

                    const float vol = 0.5f;
                    bass.SetChannelAttribute(stream.Handle, ChannelAttribute.VolumeLevel, vol);
                    Assert.Equal(vol, bass.GetChannelAttribute(stream.Handle, ChannelAttribute.VolumeLevel));
                }
            }
        }
    }
}