using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using JetBrains.Annotations;

namespace AudioSensei.Models
{
    public struct Track : IEquatable<Track>, INotifyPropertyChanged
    {
        [JsonIgnore]
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name"));
            }
        }
        [JsonIgnore]
        public string Author
        {
            get => _author;
            set
            {
                _author = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Author"));
            }
        }
        public Source Source { get; }
        [NotNull]
        public string Url { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        private string _name;
        private string _author;

        [JsonConstructor]
        public Track(Source source, [NotNull] string url)
        {
            _name = null;
            _author = null;

            Source = source;
            Url = url;
            PropertyChanged = null;

            Name = "";
            Author = "";
        }

        [Pure]
        public static Track CreateFromFile([NotNull] string filePath)
        {
            var track = new Track(Source.File, filePath);
            track.LoadMetadataFromFile();
            return track;
        }

        public void LoadMetadataFromFile()
        {
            try
            {
                var tagFile = TagLib.File.Create(Url);
                Name = string.IsNullOrEmpty(tagFile.Tag.Title)
                    ? Path.GetFileNameWithoutExtension(Url)
                    : tagFile.Tag.Title;
                Author = string.IsNullOrEmpty(tagFile.Tag.JoinedPerformers)
                    ? "Unknown"
                    : tagFile.Tag.JoinedPerformers;
            }
            catch
            {
                Name = Path.GetFileNameWithoutExtension(Url);
                Author = "Unknown";
            }
        }

        [Pure]
        public bool Equals(Track other)
        {
            return Name == other.Name && Author == other.Author && Source == other.Source && Url == other.Url;
        }

        [Pure]
        public override bool Equals(object obj)
        {
            return obj is Track other && Equals(other);
        }

        [Pure]
        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Author, (int)Source, Url);
        }

        [Pure]
        public static bool operator ==(Track left, Track right)
        {
            return left.Equals(right);
        }

        [Pure]
        public static bool operator !=(Track left, Track right)
        {
            return !left.Equals(right);
        }
    }
}
