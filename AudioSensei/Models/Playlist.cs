using Newtonsoft.Json;
using System.Collections.Generic;

namespace AudioSensei.Models
{
    public struct Playlist
    {
        public string Name { get; }
        public string Author { get; }
        public string Description { get; }

        public IReadOnlyList<Track> Tracks => tracks;

        private List<Track> tracks;

        [JsonConstructor]
        public Playlist(string name, string author, string description, List<Track> tracks)
        {
            Name = name;
            Author = author;
            Description = description;
            this.tracks = tracks;
        }
    }
}