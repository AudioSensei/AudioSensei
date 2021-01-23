using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace AudioSensei.Models
{
    public struct Playlist : IEquatable<Playlist>, INotifyPropertyChanged
    {
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name"));
            }
        }
        public string Author
        {
            get => _author;
            set
            {
                _author = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Author"));
            }
        }
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Description"));
            }
        }

        public Guid UniqueId { get; }
        public ObservableCollection<Track> Tracks { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        private string _name;
        private string _author;
        private string _description;

        [JsonConstructor]
        public Playlist(string name, Guid uniqueId, string author, string description, ObservableCollection<Track> tracks)
        {
            _name = null;
            _author = null;
            _description = null;

            UniqueId = uniqueId;
            Tracks = tracks;
            PropertyChanged = null;

            Name = name;
            Author = author;
            Description = description;
        }

        [Pure]
        public static Playlist Load(string filePath)
        {
            return JsonConvert.DeserializeObject<Playlist>(File.ReadAllText(filePath));
        }

        public void Save(string filePath)
        {
            File.WriteAllText(filePath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        [Pure]
        public bool Equals(Playlist other)
        {
            return Name == other.Name && UniqueId == other.UniqueId && Author == other.Author && Description == other.Description && Tracks.SequenceEqual(other.Tracks);
        }

        [Pure]
        public override bool Equals(object obj)
        {
            return obj is Playlist other && Equals(other);
        }

        [Pure]
        public override int GetHashCode()
        {
            return HashCode.Combine(Name, UniqueId, Author, Description, Tracks);
        }

        [Pure]
        public static bool operator ==(Playlist left, Playlist right)
        {
            return left.Equals(right);
        }

        [Pure]
        public static bool operator !=(Playlist left, Playlist right)
        {
            return !left.Equals(right);
        }
    }
}
