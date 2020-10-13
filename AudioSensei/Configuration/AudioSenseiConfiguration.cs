using System.IO;
using Newtonsoft.Json;

namespace AudioSensei.Configuration
{
    public class AudioSenseiConfiguration
    {
        private static AudioSenseiConfiguration Create(string filePath)
        {
            var configuration = new AudioSenseiConfiguration();
            configuration.Save(filePath);
            return configuration;
        }

        private static AudioSenseiConfiguration Load(string filePath)
        {
            return JsonConvert.DeserializeObject<AudioSenseiConfiguration>(File.ReadAllText(filePath));
        }

        public static AudioSenseiConfiguration LoadOrCreate(string filePath)
        {
            return File.Exists(filePath) ? Load(filePath) : Create(filePath);
        }

        public void Save(string filePath)
        {
            File.WriteAllText(filePath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}
