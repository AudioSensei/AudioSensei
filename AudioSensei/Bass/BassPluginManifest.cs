using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace AudioSensei.Bass
{
    internal class BassPluginManifest
    {
        [CanBeNull]
        public string Name { get; set; }
        [CanBeNull]
        public Dictionary<string, Dictionary<string, string>> Library { get; set; }

        public static BassPluginManifest Load([NotNull] string filePath)
        {
            return JsonConvert.DeserializeObject<BassPluginManifest>(File.ReadAllText(filePath));
        }
    }
}
