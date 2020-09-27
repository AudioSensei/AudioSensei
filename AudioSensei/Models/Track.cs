using Newtonsoft.Json;
using System;

namespace AudioSensei.Models
{
    public struct Track : IEquatable<Track>
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

        public bool Equals(Track other)
        {
            return Name == other.Name && Author == other.Author && Source == other.Source && Url == other.Url;
        }

        public override bool Equals(object obj)
        {
            return obj is Track other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Author, (int) Source, Url);
        }

        public static bool operator ==(Track left, Track right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Track left, Track right)
        {
            return !left.Equals(right);
        }
    }
}
