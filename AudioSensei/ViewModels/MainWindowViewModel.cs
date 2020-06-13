using AudioSensei.Bass;
using AudioSensei.Models;
using Avalonia.Threading;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;

namespace AudioSensei.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public float Volume
        {
            get => IsInitialized() ? BassNative.GetChannelAttribute(handle, ChannelAttribute.VolumeLevel) * 100f : 100f;
            set
            {
                if (IsInitialized())
                {
                    BassNative.SetChannelAttribute(handle, ChannelAttribute.VolumeLevel, value / 100f);
                    this.RaisePropertyChanged("Volume");
                }
            }
        }

        public int SelectedIndex
        {
            get => selectedIndex;
            set => this.RaiseAndSetIfChanged(ref selectedIndex, value, "SelectedIndex");
        }

        public int SelectedPageIndex
        {
            get => selectedPageIndex;
            set => this.RaiseAndSetIfChanged(ref selectedPageIndex, value, "SelectedPageIndex");
        }

        public string CurrentTime { get => $@"{TimeSpan.FromSeconds(GetCurrentTime()):hh\:mm\:ss}"; }
        public string TotalTime { get => $@"{TimeSpan.FromSeconds(GetTotalTime()):hh\:mm\:ss}"; }
        public int Total { get => (int)(GetCurrentTime() / GetTotalTime() * 100); }

        public ObservableCollection<Track> Tracks { get; set;  } = new ObservableCollection<Track>();
        public ObservableCollection<Playlist> Playlists { get; set; } = new ObservableCollection<Playlist>();

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

		private readonly DispatcherTimer timer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(1000.0) };
		private readonly Random random = new Random();

        private Track? previousTrack = null;

        private uint handle = 0;

        private int selectedPageIndex = 0;
        private int selectedIndex = -1;

        private bool repeat = false;
        private bool shuffle = false;
    
    	public MainWindowViewModel()
		{
            BassNative.Init();

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
            PlayOrPauseCommand = ReactiveCommand.Create(PlayOrPause);
            StopCommand = ReactiveCommand.Create(() => StopInternal());
            NextCommand = ReactiveCommand.Create(() => Next(repeat, shuffle));
            PreviousCommand = ReactiveCommand.Create(() => Previous(repeat, shuffle));
            RepeatCommand = ReactiveCommand.Create(Repeat);
            ShuffleCommand = ReactiveCommand.Create(Shuffle);
            AllTracksCommand = ReactiveCommand.Create(() => { SelectedPageIndex = 0; });
            FavouriteTracksCommand = ReactiveCommand.Create(() => { SelectedPageIndex = 1; });
            RecentlyPlayedTracksCommand = ReactiveCommand.Create(() => { SelectedPageIndex = 2; });
            AddPlaylistCommand = ReactiveCommand.Create(() => { });
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
                    var playlist = Playlists[0];

                    foreach (var track in playlist.Tracks)
                    {
                        switch (track.Source)
                        {
                            case Source.File:
                                Tracks.Add(track);
                                break;
                        }
                    }
                }
            }
        }

        private void PlayOrPause()
        {
            if (Tracks.Count > 0)
            {
                if (SelectedIndex == -1)
                {
                    SelectedIndex = 0;
                }

                if (IsPlaying())
                {
                    if (Tracks[SelectedIndex].Equals(previousTrack))
                    {
                        PauseInternal();
                    }
                    else
                    {
                        previousTrack = Tracks[SelectedIndex];
                        Play(Tracks[SelectedIndex]);
                    }
                }
                else if (IsPaused())
                {
                    if (Tracks[SelectedIndex].Equals(previousTrack))
                    {
                        previousTrack = Tracks[SelectedIndex];
                        ResumeInternal();
                    }
                    else
                    {
                        previousTrack = Tracks[SelectedIndex];
                        Play(Tracks[SelectedIndex]);
                    }
                }
                else
                {
                    previousTrack = Tracks[SelectedIndex];
                    Play(Tracks[SelectedIndex]);
                }
            }
        }

        private void Previous(bool repeat = true, bool shuffle = false)
        {
            if (Tracks.Count > 0)
            {
                if (shuffle)
                {
                    previousTrack = Tracks[SelectedIndex];
                    SelectedIndex = random.Next(0, Tracks.Count - 1);
                }
                else if (selectedIndex > 0)
                {
                    previousTrack = Tracks[SelectedIndex];
                    SelectedIndex--;
                }
                else if (repeat)
                {
                    previousTrack = Tracks[SelectedIndex];
                    SelectedIndex = Tracks.Count - 1;
                }
                else
                {
                    return;
                }

                Play(Tracks[selectedIndex]);
            }
        }

        private void Next(bool repeat = true, bool shuffle = false)
        {
            if (Tracks.Count > 0)
            {
                if (shuffle)
                {
                    previousTrack = Tracks[SelectedIndex];
                    SelectedIndex = random.Next(0, Tracks.Count - 1);
                }
                else if (selectedIndex < Tracks.Count - 1)
                {
                    previousTrack = Tracks[SelectedIndex];
                    SelectedIndex++;
                }
                else if (repeat)
                {
                    previousTrack = Tracks[SelectedIndex];
                    SelectedIndex = 0;
                }
                else
                {
                    return;
                }

                Play(Tracks[SelectedIndex]);
            }
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

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (IsInitialized())
            {
                var totalTime = BassNative.ConvertBytesToSeconds(handle, BassNative.GetChannelLength(handle, LengthMode.Bytes));
                var currentTime = BassNative.ConvertBytesToSeconds(handle, BassNative.GetChannelPosition(handle, LengthMode.Bytes));

                if (totalTime - currentTime < 0.5)
                {
                    Next();
                }

                this.RaisePropertyChanged("CurrentTime");
                this.RaisePropertyChanged("TotalTime");
                this.RaisePropertyChanged("Total");
            }
        }

        private void Play(Track track)
        {
            var previousVolume = Volume;

            if (IsPlaying())
            {
                StopInternal();
            }

            switch (track.Source)
            {
                case Source.File:
                    PlayInternal(track.Url);
                    break;
            }

            Volume = previousVolume;
        }

        private bool IsInitialized()
        {
            return handle != 0;
        }

        private bool IsPlaying()
        {
            return IsInitialized() && BassNative.GetChannelStatus(handle) == ChannelStatus.Playing;
        }

        private bool IsPaused()
        {
            return IsInitialized() && BassNative.GetChannelStatus(handle) == ChannelStatus.Paused;
        }

        private double GetCurrentTime()
        {
            return IsInitialized() ? BassNative.ConvertBytesToSeconds(handle, BassNative.GetChannelPosition(handle, LengthMode.Bytes)) : 0.0;
        }

        private double GetTotalTime()
        {
            return IsInitialized() ? BassNative.ConvertBytesToSeconds(handle, BassNative.GetChannelLength(handle, LengthMode.Bytes)) : 0.0;
        }

        private void PlayInternal(string filePath)
        {
            if (File.Exists(filePath))
            {
                if (IsInitialized())
                {
                    BassNative.StopChannel(handle);
                    BassNative.FreeStream(handle);
                }

                handle = BassNative.CreateStreamFromFile(filePath);
                BassNative.PlayChannel(handle);
            }
        }

        private void ResumeInternal()
        {
            if (IsInitialized())
            {
                BassNative.PlayChannel(handle);
            }
        }

        private void PauseInternal()
        {
            if (IsInitialized())
            {
                BassNative.PauseChannel(handle);
            }
        }

        private void StopInternal()
        {
            if (IsInitialized())
            {
                BassNative.StopChannel(handle);
                BassNative.FreeStream(handle);
            }
        }
	}
}