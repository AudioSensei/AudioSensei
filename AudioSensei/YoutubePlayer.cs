using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.ClosedCaptions;
using YoutubeExplode.Videos.Streams;

namespace AudioSensei
{
    public class YoutubePlayer
    {
        private readonly YoutubeClient _client;
        private readonly IAudioBackend _backend;

        public YoutubePlayer(IAudioBackend backend)
        {
            _client = new YoutubeClient(WebHelper.CreateHttpClient(true));
            _backend = backend;
        }

        public async Task<YoutubeInfo> Play(string url)
        {
            var b = await _client.Videos.GetAsync(url);
            var c = await _client.Videos.ClosedCaptions.GetManifestAsync(b.Id);
            var s = await _client.Videos.Streams.GetManifestAsync(b.Id);

            var link = new Uri(s.GetAudioOnly().WithHighestBitrate().Url);

            var a = _backend.Play(link);

            return new YoutubeInfo
            {
                Video = b,
                Captions = c.Tracks,
                Url = link,
                AudioStream = a
            };
        }

        public struct YoutubeInfo
        {
            public Video Video;
            public IReadOnlyList<ClosedCaptionTrackInfo> Captions;
            public Uri Url;
            public IAudioStream AudioStream;
        }
    }
}
