using System.IO;
using Newtonsoft.Json;

namespace AudioSensei.Configuration
{
    public class AudioSenseiConfiguration
    {
        public static AudioSenseiConfiguration Create(string filePath)
        {
            var configuration = new AudioSenseiConfiguration();
            configuration.Save(filePath);
            return configuration;
        }

        public static AudioSenseiConfiguration Load(string filePath)
        {
            return JsonConvert.DeserializeObject<AudioSenseiConfiguration>(File.ReadAllText(filePath));
        }

        public static AudioSenseiConfiguration LoadOrCreate(string filePath)
        {
            if (File.Exists(filePath))
            {
                return Load(filePath);
            }
            else
            {
                return Create(filePath);
            }
        }

        public void Save(string filePath)
        {
            File.WriteAllText(filePath, JsonConvert.SerializeObject(this));
        }
    }
}
