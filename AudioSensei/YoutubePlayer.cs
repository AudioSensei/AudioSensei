using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
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
            _client = new YoutubeClient(WebHelper.CreateHttpClient());
            _backend = backend;
        }

        public async Task<YoutubeInfo> GetInfo([NotNull] string url)
        {
            var video = await _client.Videos.GetAsync(url);
            var captionManifest = await _client.Videos.ClosedCaptions.GetManifestAsync(video.Id);
            var streamManifest = await _client.Videos.Streams.GetManifestAsync(video.Id);
            var link = new Uri(streamManifest.GetAudioOnly().WithHighestBitrate().Url);

            return new YoutubeInfo
            {
                Video = video,
                Captions = captionManifest.Tracks,
                Url = link,
                AudioStream = null
            };
        }

        public async Task<YoutubeInfo> Play([NotNull] string url)
        {
            var info = await GetInfo(url);
            info.AudioStream = _backend.Play(info.Url);
            return info;
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
