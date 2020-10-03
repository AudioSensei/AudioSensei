using AudioSensei.Bass;
using Xunit;

namespace AudioSensei.Tests
{
    public class BassTests
    {
        [Fact]
        public void TestInitialization()
        {
            BassNative.Initialize();
            BassNative.Free();
        }

        [Fact]
        public void TestPlayback()
        {
            BassNative.Initialize();
            
            var handle = BassNative.CreateStreamFromFile("test.wav");
            handle.PlayChannel();
            Assert.NotEqual(handle, BassHandle.Null);
            Assert.Equal(ChannelStatus.Playing, handle.GetChannelStatus());

            handle.PauseChannel();
            Assert.Equal(ChannelStatus.Paused, handle.GetChannelStatus());
            
            handle.StopChannel();
            Assert.Equal(ChannelStatus.Stopped, handle.GetChannelStatus());
            
            BassNative.Free();
        }

        [Fact]
        public void TestChannelAttributes()
        {
            BassNative.Initialize();
            
            var handle = BassNative.CreateStreamFromFile("test.wav");
            handle.PlayChannel();

            handle.SetChannelAttribute(ChannelAttribute.VolumeLevel, 0.5f);
            Assert.Equal(0.5f, handle.GetChannelAttribute(ChannelAttribute.VolumeLevel));
            
            handle.StopChannel();
            BassNative.Free();
        }
    }
}