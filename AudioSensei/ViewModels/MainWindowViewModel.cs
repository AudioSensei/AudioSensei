using AudioSensei.Bass;
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
        // REDESIGN
        private bool isSidebarVisible = true;
        public bool IsSidebarVisible
        {
            get => isSidebarVisible;
            set {
                this.RaiseAndSetIfChanged(ref isSidebarVisible, value, nameof(IsSidebarVisible));
                SidebarWidth = value ? 200 : 0;
            }
        }
        private int sidebarWidth = 200;
        public int SidebarWidth
        {
            get => sidebarWidth;
            set => this.RaiseAndSetIfChanged(ref sidebarWidth, value, nameof(SidebarWidth));
        }
        public ReactiveCommand<Unit, Unit> ToggleSidebarCommand { get; private set; }

        // Player
        public float Volume
        {
            get => IsInitialized() ? handle.GetChannelAttribute(ChannelAttribute.VolumeLevel) * 100f : 100f;
            set
            {
                if (IsInitialized())
                {
                    handle.SetChannelAttribute(ChannelAttribute.VolumeLevel, value / 100f);
                    this.RaisePropertyChanged("Volume");
                }
            }
        }
        public string CurrentTime { get => $@"{TimeSpan.FromSeconds(GetCurrentTime()):hh\:mm\:ss}"; }
        public string TotalTime { get => $@"{TimeSpan.FromSeconds(GetTotalTime()):hh\:mm\:ss}"; }
        public int Total { get => (int)(GetCurrentTime() / GetTotalTime() * 100); }

        // Pages
        public int SelectedPageIndex
        {
            get => selectedPageIndex;
            set => this.RaiseAndSetIfChanged(ref selectedPageIndex, value, "SelectedPageIndex");
        }

        // Playlist creator
        public bool IsPlaylistCreatorVisible
        {
            get => isPlaylistCreatorVisible;
            set => this.RaiseAndSetIfChanged(ref isPlaylistCreatorVisible, value, "IsPlaylistCreatorVisible");
        }
        public string PlaylistName
        {
            get => playlistName;
            set => this.RaiseAndSetIfChanged(ref playlistName, value, "PlaylistName");
        }
        public string PlaylistDescription
        {
            get => playlistDescription;
            set => this.RaiseAndSetIfChanged(ref playlistDescription, value, "PlaylistDescription");
        }
        public string PlaylistAuthor
        {
            get => playlistAuthor;
            set => this.RaiseAndSetIfChanged(ref playlistAuthor, value, "PlaylistAuthor");
        }

        // Playlists
        public ObservableCollection<Playlist> Playlists { get; set; } = new ObservableCollection<Playlist>();
        public Playlist? CurrentlyPlayedPlaylist
        {
            get => currentlyPlayedPlaylist;
            set => this.RaiseAndSetIfChanged(ref currentlyPlayedPlaylist, value, "CurrentlyPlayedPlaylist");
        }
        public Playlist CurrentlyVisiblePlaylist
        {
            get => currentlyVisiblePlaylist;
            set
            {
                this.RaiseAndSetIfChanged(ref currentlyVisiblePlaylist, value, "CurrentlyVisiblePlaylist");

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
            set => this.RaiseAndSetIfChanged(ref selectedTrackIndex, value, "SelectedTrackIndex");
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

        private readonly DispatcherTimer timer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(1000.0) };
        private readonly Random random = new Random();

        // Player
        private BassHandle handle = BassHandle.Null;

        private bool repeat = false;
        private bool shuffle = false;

        // Pages
        private int selectedPageIndex = 0;

        // Playlist creator
        private bool isPlaylistCreatorVisible = false;
        private string playlistName = "";
        private string playlistAuthor = "";
        private string playlistDescription = "";

        // Playlists
        private Playlist currentlyVisiblePlaylist = new Playlist("", Guid.NewGuid(), "", "", new ObservableCollection<Track>());
        private Playlist? currentlyPlayedPlaylist = null;

        // Tracks
        private int selectedTrackIndex = -1;
    
        public MainWindowViewModel()
        {
            BassNative.Initialize();

            InitializeCommands();
            LoadPlaylists();

            timer.Tick += Timer_Tick;
            timer.Start();
        }

        ~MainWindowViewModel()
        {
            StopInternal();
            BassNative.Free();
        }

        private void InitializeCommands()
        {
            // Redesign v
            ToggleSidebarCommand = ReactiveCommand.Create(() => { IsSidebarVisible = !IsSidebarVisible; });
            // Redesign ^
            PlayOrPauseCommand = ReactiveCommand.Create(PlayOrPause);
            StopCommand = ReactiveCommand.Create(StopInternal);
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
            if (Directory.Exists("Playlists"))
            {
                foreach (var file in Directory.GetFiles("Playlists"))
                {
                    if (file.EndsWith(".json"))
                    {
                        Playlists.Add(JsonConvert.DeserializeObject<Playlist>(File.ReadAllText(file)));
                    }
                }

                if (Playlists.Count > 0)
                {
                    CurrentlyVisiblePlaylist = Playlists[0];
                }
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

            if (IsPlaying() && currentlyPlayedPlaylist == currentlyVisiblePlaylist && CurrentTrackIndex == SelectedTrackIndex)
            {
                PauseInternal();
                return;
            }
            else if (IsPaused() && currentlyPlayedPlaylist == currentlyVisiblePlaylist && CurrentTrackIndex == SelectedTrackIndex)
            {
                ResumeInternal();
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

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (IsInitialized())
            {
                var totalTime = handle.ConvertBytesToSeconds(handle.GetChannelLength(LengthMode.Bytes));
                var currentTime = handle.ConvertBytesToSeconds(handle.GetChannelPosition(LengthMode.Bytes));

                if (totalTime - currentTime < 0.5)
                {
                    Next();
                }

                this.RaisePropertyChanged("CurrentTime");
                this.RaisePropertyChanged("TotalTime");
                this.RaisePropertyChanged("Total");
            }
        }

        private void Play(Track? track)
        {
            if (track == null) 
                throw new ArgumentNullException(nameof(track));

            var previousVolume = Volume;

            switch (track?.Source)
            {
                case Source.File:
                    PlayInternal(track?.Url);
                    break;
            }

            Volume = previousVolume;
        }

        private bool IsInitialized()
        {
            return handle != BassHandle.Null;
        }

        private bool IsPlaying()
        {
            return IsInitialized() && handle.GetChannelStatus() == ChannelStatus.Playing;
        }

        private bool IsPaused()
        {
            return IsInitialized() && handle.GetChannelStatus() == ChannelStatus.Paused;
        }

        private double GetCurrentTime()
        {
            return IsInitialized() ? handle.ConvertBytesToSeconds(handle.GetChannelPosition(LengthMode.Bytes)) : 0.0;
        }

        private double GetTotalTime()
        {
            return IsInitialized() ? handle.ConvertBytesToSeconds(handle.GetChannelLength(LengthMode.Bytes)) : 0.0;
        }

        private void PlayInternal(string filePath)
        {
            if (File.Exists(filePath))
            {
                if (IsInitialized())
                {
                    handle.StopChannel();
                }

                handle = BassNative.CreateStreamFromFile(filePath);
                handle.PlayChannel();
            }
        }

        private void ResumeInternal()
        {
            if (IsInitialized())
            {
                handle.PlayChannel();
            }
        }

        private void PauseInternal()
        {
            if (IsInitialized())
            {
                handle.PauseChannel();
            }
        }

        private void StopInternal()
        {
            if (IsInitialized())
            {
                handle.StopChannel();
                handle.FreeStream();
            }
        }
    }
}
