using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace AudioSensei.Models
{
    public sealed class Track : IEquatable<Track>, INotifyPropertyChanged
    {
        [JsonIgnore]
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
        [JsonIgnore]
        public string Author
        {
            get => _author;
            set
            {
                _author = value;
                OnPropertyChanged(nameof(Author));
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
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _name == other._name && _author == other._author && Source == other.Source && Url == other.Url;
        }

        [Pure]
        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is Track other && Equals(other);
        }

        [Pure]
        public override int GetHashCode()
        {
            return HashCode.Combine(_name, _author, (int) Source, Url);
        }

        [Pure]
        public static bool operator ==(Track left, Track right)
        {
            return Equals(left, right);
        }

        [Pure]
        public static bool operator !=(Track left, Track right)
        {
            return !Equals(left, right);
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            else
            {
                Dispatcher.UIThread.Post(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
            }
        }
    }
}
