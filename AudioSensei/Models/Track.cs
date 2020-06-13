using Newtonsoft.Json;

namespace AudioSensei.Models
{
    public struct Track
    {
        [JsonIgnore]
        public string Name { get; set; }
        public string Author { get; set; }
        public Source Source { get; }
        public string Url { get; }

        [JsonConstructor]
        public Track(string author, Source source, string url)
        {
            Name = "";
            Author = author;
            Source = source;
            Url = url;
        }
    }
}