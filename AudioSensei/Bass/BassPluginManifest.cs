using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AudioSensei.Bass
{
    internal class BassPluginManifest
    {
        public string Name { get; set; }
        public Dictionary<string, Dictionary<string, string>> Library { get; set; }

        public static BassPluginManifest Load(string filePath)
        {
            return JsonConvert.DeserializeObject<BassPluginManifest>(File.ReadAllText(filePath));
        }
    }
}
