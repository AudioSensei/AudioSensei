using Newtonsoft.Json;

namespace AudioSensei.Models
{
    public struct Track
    {
        [JsonIgnore]
        public string Name { get; set; }
        public Source Source { get; }
        public string Url { get; }

        [JsonConstructor]
        public Track(Source source, string url)
        {
            Name = "";
            Source = source;
            Url = url;
        }
    }
}