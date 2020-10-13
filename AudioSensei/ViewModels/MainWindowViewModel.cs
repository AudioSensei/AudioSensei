using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AudioSensei.Models;
using Avalonia.Threading;
using Newtonsoft.Json;
using ReactiveUI;

namespace AudioSensei.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        // Pages
        public int SelectedPageIndex
        {
            get => selectedPageIndex;
            set => this.RaiseAndSetIfChanged(ref selectedPageIndex, value, nameof(SelectedPageIndex));
        }

        // Playlist creator
        public bool IsPlaylistCreatorVisible
        {
            get => isPlaylistCreatorVisible;
            set => this.RaiseAndSetIfChanged(ref isPlaylistCreatorVisible, value, nameof(IsPlaylistCreatorVisible));
        }
        public string PlaylistName
        {
            get => playlistName;
            set => this.RaiseAndSetIfChanged(ref playlistName, value, nameof(PlaylistName));
        }
        public string PlaylistDescription
        {
            get => playlistDescription;
            set => this.RaiseAndSetIfChanged(ref playlistDescription, value, nameof(PlaylistDescription));
        }
        public string PlaylistAuthor
        {
            get => playlistAuthor;
            set => this.RaiseAndSetIfChanged(ref playlistAuthor, value, nameof(PlaylistAuthor));
        }

        // Playlists
        public ObservableCollection<Playlist> Playlists { get; set; } = new ObservableCollection<Playlist>();
        public Playlist? CurrentlyPlayedPlaylist
        {
            get => currentlyPlayedPlaylist;
            set => this.RaiseAndSetIfChanged(ref currentlyPlayedPlaylist, value, nameof(CurrentlyPlayedPlaylist));
        }
        public Playlist CurrentlyVisiblePlaylist
        {
            get => currentlyVisiblePlaylist;
            set
            {
                this.RaiseAndSetIfChanged(ref currentlyVisiblePlaylist, value, nameof(CurrentlyVisiblePlaylist));

                if (currentlyPlayedPlaylist == currentlyVisiblePlaylist)
                {
                    SelectedTrackIndex = CurrentTrackIndex;
                }
            }
        }

        //Tracks
        public int SelectedTrackIndex
        {
            get => selectedTrackIndex;
            set => this.RaiseAndSetIfChanged(ref selectedTrackIndex, value, nameof(SelectedTrackIndex));
        }
        public int CurrentTrackIndex { get; set; } = -1;

        // Current Stream
        public string CurrentTimeFormatted => AudioStream == null ? "00:00" : AudioStream.CurrentTime.ToPlaybackPosition();
        public string TotalTimeFormatted => AudioStream == null ? "00:00" : AudioStream.TotalTime.ToPlaybackPosition();
        public int Total => AudioStream == null ? 0 : (int)(AudioStream.CurrentTime / AudioStream.TotalTime * 100);

        // Commands
        public ReactiveCommand<Unit, Unit> PlayOrPauseCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> StopCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> NextCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> PreviousCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> RepeatCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> ShuffleCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> AllTracksCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> FavouriteTracksCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> RecentlyPlayedTracksCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> AddPlaylistCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> CreatePlaylistCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> CancelPlaylistCreationCommand { get; private set; }
        public ReactiveCommand<Guid, Unit> SelectPlaylistCommand { get; private set; }

        public IAudioBackend AudioBackend { get; }
        public YoutubePlayer YoutubePlayer { get; }

        public IAudioStream AudioStream { get; private set; }

        private readonly DispatcherTimer timer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(100.0) };
        private readonly Random random = new Random(RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue));

        private bool repeat;
        private bool shuffle;

        // Pages
        private int selectedPageIndex;

        // Playlist creator
        private bool isPlaylistCreatorVisible;
        private string playlistName = "";
        private string playlistAuthor = "";
        private string playlistDescription = "";

        // Playlists
        private Playlist currentlyVisiblePlaylist = new Playlist("", Guid.NewGuid(), "", "", new ObservableCollection<Track>());
        private Playlist? currentlyPlayedPlaylist;

        // Tracks
        private int selectedTrackIndex = -1;
    
        public MainWindowViewModel(IAudioBackend audioBackend)
        {
            AudioBackend = audioBackend;
            YoutubePlayer = new YoutubePlayer(audioBackend);

            InitializeCommands();
            LoadPlaylists();

            timer.Tick += Tick;
        }

        ~MainWindowViewModel()
        {
            AudioStream?.Dispose();
            AudioStream = null;
            AudioBackend.Dispose();
        }

        private void InitializeCommands()
        {
            PlayOrPauseCommand = ReactiveCommand.CreateFromTask(PlayOrPause);
            StopCommand = ReactiveCommand.Create(Stop);
            NextCommand = ReactiveCommand.CreateFromTask(async () => await Next(repeat, shuffle));
            PreviousCommand = ReactiveCommand.CreateFromTask(async () => await Previous(repeat, shuffle));
            RepeatCommand = ReactiveCommand.Create(Repeat);
            ShuffleCommand = ReactiveCommand.Create(Shuffle);
            AllTracksCommand = ReactiveCommand.Create(() => { SelectedPageIndex = 0; });
            FavouriteTracksCommand = ReactiveCommand.Create(() => { SelectedPageIndex = 1; });
            RecentlyPlayedTracksCommand = ReactiveCommand.Create(() => { SelectedPageIndex = 2; });
            AddPlaylistCommand = ReactiveCommand.Create(() => { IsPlaylistCreatorVisible = true; });
            CreatePlaylistCommand = ReactiveCommand.Create(CreatePlaylist);
            CancelPlaylistCreationCommand = ReactiveCommand.Create(CancelPlaylistCreation);
            SelectPlaylistCommand = ReactiveCommand.Create<Guid>(SelectPlaylist);
        }

        private void LoadPlaylists()
        {
            string playlistPath = Path.Combine(App.ApplicationDataPath, "Playlists");
            if (!Directory.Exists(playlistPath))
            {
                Directory.CreateDirectory(playlistPath);
            }

            foreach (var file in Directory.EnumerateFiles(playlistPath, "*.json"))
            {
                var playlist = JsonConvert.DeserializeObject<Playlist>(File.ReadAllText(file));

                for (int i = 0; i < playlist.Tracks.Count; i++)
                {
                    var track = playlist.Tracks[i];

                    switch (track.Source)
                    {
                        case Source.File:
                            try
                            {
                                TagLib.File tagFile = TagLib.File.Create(track.Url);
                                track.Name = string.IsNullOrEmpty(tagFile.Tag.Title)
                                    ? Path.GetFileNameWithoutExtension(track.Url)
                                    : tagFile.Tag.Title;
                                track.Author = string.IsNullOrEmpty(tagFile.Tag.JoinedPerformers)
                                    ? "Unknown"
                                    : tagFile.Tag.JoinedPerformers;
                            }
                            catch
                            {
                                track.Name = Path.GetFileNameWithoutExtension(track.Url);
                                track.Author = "Unknown";
                            }

                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    playlist.Tracks[i] = track;
                }

                Playlists.Add(playlist);
            }

            if (Playlists.Count > 0)
            {
                CurrentlyVisiblePlaylist = Playlists[0];
            }
        }

        private async Task PlayOrPause()
        {
            currentlyPlayedPlaylist ??= currentlyVisiblePlaylist;

            if (currentlyPlayedPlaylist?.Tracks!.Count == 0)
            {
                return;
            }

            if (CurrentTrackIndex == -1)
            {
                if (SelectedTrackIndex != -1)
                {
                    CurrentTrackIndex = SelectedTrackIndex;
                }
                else
                {
                    CurrentTrackIndex = 0;

                    if (currentlyPlayedPlaylist == currentlyVisiblePlaylist)
                    {
                        SelectedTrackIndex = 0;
                    }
                }
            }


            if (currentlyPlayedPlaylist == currentlyVisiblePlaylist && CurrentTrackIndex == SelectedTrackIndex)
            {
                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (AudioStream.Status)
                {
                    case AudioStreamStatus.Playing:
                        AudioStream.Pause();
                        timer.Stop();
                        return;
                    case AudioStreamStatus.Paused:
                        AudioStream.Resume();
                        timer.Start();
                        return;
                }
            }

            if (currentlyPlayedPlaylist != currentlyVisiblePlaylist)
            {
                currentlyPlayedPlaylist = currentlyVisiblePlaylist;

                if (SelectedTrackIndex == -1)
                {
                    CurrentTrackIndex = 0;
                    SelectedTrackIndex = 0;
                }
                else
                {
                    CurrentTrackIndex = SelectedTrackIndex;
                }
            }

            var track = currentlyPlayedPlaylist?.Tracks[CurrentTrackIndex];

            if (track == null)
                throw new ArgumentNullException(nameof(track));

            await Play(track.Value);
            timer.Start();
        }

        private void Stop()
        {
            AudioStream?.Dispose();
            AudioStream = null;
            timer.Stop();
        }

        private async Task Previous(bool repeat = true, bool shuffle = false)
        {
            if (currentlyPlayedPlaylist == null)
            {
                return;
            }

            if (shuffle)
            {
                CurrentTrackIndex = random.Next(0, currentlyPlayedPlaylist?.Tracks!.Count - 1);
            }
            else if (CurrentTrackIndex > 0)
            {
                CurrentTrackIndex--;
            }
            else if (repeat)
            {
                CurrentTrackIndex = currentlyPlayedPlaylist?.Tracks!.Count - 1;
            }
            else
            {
                return;
            }

            if (currentlyPlayedPlaylist == currentlyVisiblePlaylist)
            {
                SelectedTrackIndex = CurrentTrackIndex;
            }

            var track = currentlyPlayedPlaylist?.Tracks[CurrentTrackIndex];

            if (track == null)
                throw new ArgumentNullException(nameof(track));

            await Play(track.Value);
        }

        private async Task Next(bool repeat = true, bool shuffle = false)
        {
            if (currentlyPlayedPlaylist == null)
            {
                return;
            }

            if (shuffle)
            {
                CurrentTrackIndex = random.Next(0, (currentlyPlayedPlaylist?.Tracks?.Count ?? 0) - 1);
            }
            else if (CurrentTrackIndex < (currentlyPlayedPlaylist?.Tracks?.Count ?? 0) - 1)
            {
                CurrentTrackIndex++;
            }
            else if (repeat)
            {
                CurrentTrackIndex = 0;
            }
            else
            {
                return;
            }

            if (currentlyPlayedPlaylist == currentlyVisiblePlaylist)
            {
                SelectedTrackIndex = CurrentTrackIndex;
            }

            var track = currentlyPlayedPlaylist?.Tracks?[CurrentTrackIndex];

            if (track == null)
                throw new ArgumentNullException(nameof(track));

            await Play(track.Value);
        }

        private void Shuffle()
        {
            if (shuffle)
            {
                shuffle = false;
            }
            else
            {
                shuffle = true;
                repeat = false;
            }
        }

        private void Repeat()
        {
            if (repeat)
            {
                repeat = false;
            }
            else
            {
                repeat = true;
                shuffle = false;
            }
        }

        private void CreatePlaylist()
        {
            if (!string.IsNullOrWhiteSpace(playlistName))
            {
                Playlists.Add(new Playlist(playlistName, Guid.NewGuid(), playlistAuthor, playlistDescription, new ObservableCollection<Track>()));
            }

            CancelPlaylistCreation();
        }

        private void CancelPlaylistCreation()
        {
            IsPlaylistCreatorVisible = false;
            PlaylistName = "";
            PlaylistAuthor = "";
            PlaylistDescription = "";
        }

        private void SelectPlaylist(Guid uniqueId)
        {
            CurrentlyVisiblePlaylist = Playlists.First(playlist => playlist.UniqueId == uniqueId);
        }

        private async Task Play(Track track)
        {
            AudioStream?.Dispose();
            AudioStream = null;

            AudioStream = track.Source switch
            {
                Source.File => AudioBackend.Play(new Uri(track.Url)),
                Source.YouTube => (await YoutubePlayer.Play(track.Url)).AudioStream,
                _ => throw new NotImplementedException()
            };
        }
        
        private async void Tick(object sender, EventArgs args)
        {
            if (AudioStream.Status != AudioStreamStatus.Playing)
            {
                return;
            }

            if (AudioStream.TotalTime - AudioStream.CurrentTime < TimeSpan.FromSeconds(0.5))
            {
                await Next();
            }
        }
    }
}
