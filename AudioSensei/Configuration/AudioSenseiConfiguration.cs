using System.IO;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace AudioSensei.Configuration
{
    public sealed class AudioSenseiConfiguration
    {
        [NotNull]
        public GeneralConfiguration General { get; set; } = new GeneralConfiguration();
        [NotNull]
        public PlayerConfiguration Player { get; set; } = new PlayerConfiguration();
        [NotNull]
        public BassConfiguration Bass { get; set; } = new BassConfiguration();
    
        [NotNull]
        [Pure]
        private static AudioSenseiConfiguration Create([NotNull] string filePath)
        {
            var configuration = new AudioSenseiConfiguration();
            configuration.Save(filePath);
            return configuration;
        }

        [NotNull]
        [Pure]
        private static AudioSenseiConfiguration Load([NotNull] string filePath)
        {
            return JsonConvert.DeserializeObject<AudioSenseiConfiguration>(File.ReadAllText(filePath));
        }

        [NotNull]
        [PublicAPI]
        [Pure]
        public static AudioSenseiConfiguration LoadOrCreate([NotNull] string filePath)
        {
            return File.Exists(filePath) ? Load(filePath) : Create(filePath);
        }

        [PublicAPI]
        public void Save([NotNull] string filePath)
        {
            File.WriteAllText(filePath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}
