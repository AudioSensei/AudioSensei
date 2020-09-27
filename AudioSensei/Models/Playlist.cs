using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AudioSensei.Models
{
    public struct Playlist : IEquatable<Playlist>
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

        public bool Equals(Playlist other)
        {
            return Equals(tracks, other.tracks) && Name == other.Name && Author == other.Author && Description == other.Description;
        }

        public override bool Equals(object obj)
        {
            return obj is Playlist other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(tracks, Name, Author, Description);
        }

        public static bool operator ==(Playlist left, Playlist right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Playlist left, Playlist right)
        {
            return !left.Equals(right);
        }
    }
}
