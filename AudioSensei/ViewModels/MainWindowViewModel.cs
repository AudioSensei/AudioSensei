using AudioSensei.Models;
using Avalonia.Threading;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;

namespace AudioSensei.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public IAudioBackend AudioBackend { get; }
        public YoutubePlayer YoutubePlayer { get; }

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

        private readonly DispatcherTimer timer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(100.0) };
        private readonly Random random = new Random();

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
            this.AudioBackend = audioBackend;
            YoutubePlayer = new YoutubePlayer(audioBackend);

            InitializeCommands();
            LoadPlaylists();

            timer.Tick += Tick;
        }

        ~MainWindowViewModel()
        {
            AudioBackend.Dispose();
        }

        private void InitializeCommands()
        {
            PlayOrPauseCommand = ReactiveCommand.Create(PlayOrPause);
            StopCommand = ReactiveCommand.Create(Stop);
            NextCommand = ReactiveCommand.Create(() => Next(repeat, shuffle));
            PreviousCommand = ReactiveCommand.Create(() => Previous(repeat, shuffle));
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
            if (!Directory.Exists("Playlists"))
            {
                Directory.CreateDirectory("Playlists");
            }

            foreach (var file in Directory.EnumerateFiles("Playlists", "*.json"))
            {
                var playlist = Playlist.Load(file);

                for (int i = 0; i < playlist.Tracks.Count; i++)
                {
                    var track = playlist.Tracks[i];

                    switch (track.Source)
                    {
                        case Source.File:
                            track.LoadMetadataFromFile();
                            break;
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

        private void PlayOrPause()
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

            if (AudioBackend.IsPlaying && currentlyPlayedPlaylist == currentlyVisiblePlaylist && CurrentTrackIndex == SelectedTrackIndex)
            {
                AudioBackend.Pause();
                timer.Stop();
                return;
            }

            if (AudioBackend.IsPaused && currentlyPlayedPlaylist == currentlyVisiblePlaylist && CurrentTrackIndex == SelectedTrackIndex)
            {
                AudioBackend.Resume();
                timer.Start();
                return;
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

            Play(currentlyPlayedPlaylist?.Tracks[CurrentTrackIndex]);
            timer.Start();
        }

        private void Stop()
        {
            AudioBackend.Stop();
            timer.Stop();
        }

        private void Previous(bool repeat = true, bool shuffle = false)
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

            Play(currentlyPlayedPlaylist?.Tracks[CurrentTrackIndex]);
        }

        private void Next(bool repeat = true, bool shuffle = false)
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

            Play(currentlyPlayedPlaylist?.Tracks[CurrentTrackIndex]);
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

        private void Play(Track? track)
        {
            if (track == null) 
                throw new ArgumentNullException(nameof(track));

            var previousVolume = AudioBackend.Volume;

            switch (track?.Source)
            {
                case Source.File:
                    AudioBackend.PlayFile(track?.Url);
                    break;
                case Source.YouTube:
                    // TODO: await this
                    YoutubePlayer.Play(track?.Url);
                    break;
            }

            AudioBackend.Volume = previousVolume;
        }
        
        private void Tick(object sender, EventArgs args)
        {
            if (!AudioBackend.IsInitialized)
            {
                return;
            }

            if (AudioBackend.TotalTime - AudioBackend.CurrentTime < TimeSpan.FromSeconds(0.5))
            {
                Next();
            }
        }
    }
}
