using AudioSensei.Models;
using Avalonia.Threading;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Buffers;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Serilog;

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

        // IPC
        private const int ProtocolVersion = 1;

        public MainWindowViewModel(IAudioBackend audioBackend)
        {
            this.AudioBackend = audioBackend;
            YoutubePlayer = new YoutubePlayer(audioBackend);

            InitializeCommands();
            LoadPlaylists();

            timer.Tick += Tick;

            ProcessStartupData();
        }

        ~MainWindowViewModel()
        {
            AudioBackend.Dispose();
            Program.TriggerExit();
        }

        private void ProcessStartupData()
        {
            var cmd = Environment.GetCommandLineArgs().Skip(1).ToArray();

            SetupPlaybackServer(cmd);

            foreach (var s in cmd)
            {
                // TODO: fix
                if (File.Exists(s))
                {
                    AudioBackend.PlayFile(s);
                }
            }
        }

        private void SetupPlaybackServer(string[] cmd)
        {
            // don't question locking a mutex, had to do it in order to stop VS from joining a satanistic cult of 666 and deadlocking
            var mutexStatus = new ThreadLocal<bool>();
            var tcpMutex = new Mutex(false, "AudioSenseiTcpSyncMutex");
            var mutexLock = new object();

            lock (mutexLock)
            {
                mutexStatus.Value = tcpMutex.WaitOne();
            }

            void ReleaseMutex()
            {
                lock (mutexLock)
                {
                    if (mutexStatus?.Value ?? false)
                    {
                        tcpMutex?.ReleaseMutex();
                        mutexStatus.Value = false;
                    }
                }
            }

            Program.Exit += ReleaseMutex;

            try
            {
                var lockPath = Path.Combine(App.ApplicationDataPath, "instancelock.txt");

                if (File.Exists(lockPath) && ushort.TryParse(File.ReadAllText(lockPath), out var port) && SendPlaybackRequest(port, cmd))
                {
                    lock (mutexLock)
                    {
                        ReleaseMutex();

                        Program.TriggerExit();
                        Environment.Exit(0);
                    }
                }

                try
                {
                    var tcp = new TcpListener(IPAddress.Loopback, 0);
                    tcp.Start();
                    ProcessTcp(tcp);
                    var endPoint = (IPEndPoint)tcp.LocalEndpoint;
                    File.WriteAllText(lockPath, endPoint.Port.ToString());
                    Log.Information($"Listening to playback requests on {endPoint}");

                    Program.Exit += () =>
                    {
                        lock (mutexLock)
                        {
                            if (!(mutexStatus?.Value ?? false))
                            {
                                mutexStatus.Value = tcpMutex.WaitOne();
                            }

                            Log.Information("Shutting down the playback server");
                            File.Delete(lockPath);
                            tcp.Stop();

                            ReleaseMutex();

                            mutexStatus.Dispose();
                            mutexStatus = null;
                            tcpMutex.Dispose();
                            tcpMutex = null;
                        }
                    };
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to start the playback server!");
                }
            }
            finally
            {
                ReleaseMutex();
            }
        }

        private static bool SendPlaybackRequest(ushort port, string[] paths)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    try
                    {
                        client.Connect(new IPEndPoint(IPAddress.Loopback, port));
                        Log.Information($"Connected to a playback server on port {port}");
                    }
                    catch (SocketException se)
                    {
                        Log.Information(se, $"Failed to connect to a playback server on port {port}");
                        return false;
                    }
                    using (var stream = client.GetStream())
                    {
                        int version = ProtocolVersion;
                        Span<byte> intSpan = stackalloc byte[sizeof(int)];
                        MemoryMarshal.Write(intSpan, ref version);
                        stream.Write(intSpan);

                        var pathCount = paths.Length;
                        MemoryMarshal.Write(intSpan, ref pathCount);
                        stream.Write(intSpan);

                        var source = Source.File;
                        Span<byte> sourceSpan = stackalloc byte[sizeof(Source)];
                        MemoryMarshal.Write(sourceSpan, ref source);

                        for (int i = 0; i < pathCount; i++)
                        {
                            var path = paths[i];
                            var buffer = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(path.Length));
                            var length = Encoding.UTF8.GetBytes(path, 0, path.Length, buffer, 0);
                            MemoryMarshal.Write(intSpan, ref length);
                            stream.Write(intSpan);
                            stream.Write(buffer, 0, length);
                            stream.Write(sourceSpan);
                            ArrayPool<byte>.Shared.Return(buffer);
                        }
                    }
                }
                Log.Information($"Send a request to a playback server on port {port}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Failed to send data to a playback server on port {port}");
                return false;
            }
        }

        private async void ProcessTcp(TcpListener listener)
        {
            var intBuffer = new byte[sizeof(int)];
            var sourceBuffer = new byte[sizeof(Source)];
            while (true)
            {
                try
                {
                    using (var client = await listener.AcceptTcpClientAsync())
                    {
                        Log.Information($"Connection from {client.Client.RemoteEndPoint} received!");
                        await using (var stream = client.GetStream())
                        {
                            await stream.ReadAsync(intBuffer);
                            var version = MemoryMarshal.Read<int>(intBuffer);
                            if (version != ProtocolVersion)
                            {
                                Log.Information($"Rejecting playback request with a different protocol version (local: {ProtocolVersion}, remote {version})");
                                continue;
                            }

                            App.MainWindow.SetForegroundWindow();

                            await stream.ReadAsync(intBuffer);
                            var pathCount = MemoryMarshal.Read<int>(intBuffer);
                            Log.Information($"Reading {pathCount} playback requests");

                            for (int i = 0; i < pathCount; i++)
                            {
                                await stream.ReadAsync(intBuffer);
                                var length = MemoryMarshal.Read<int>(intBuffer);
                                var buffer = ArrayPool<byte>.Shared.Rent(length);
                                await stream.ReadAsync(new Memory<byte>(buffer, 0, length));
                                var path = Encoding.UTF8.GetString(buffer, 0, length);
                                ArrayPool<byte>.Shared.Return(buffer);
                                await stream.ReadAsync(sourceBuffer);
                                var source = MemoryMarshal.Read<Source>(sourceBuffer);

                                Log.Information($"Received a playback request for {path} from {source}");
                                switch (source)
                                {
                                    // TODO: ass the path into some list
                                    case Source.File when File.Exists(path):
                                        AudioBackend.PlayFile(path);
                                        break;
                                    case Source.YouTube:
                                        YoutubePlayer.Play(path);
                                        break;
                                    default:
                                        Log.Warning($"Received invalid source ({source}) for playback from {path}");
                                        break;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Receiving playback request failed");
                }

                await Task.Delay(10);
            }
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
            string playlistPath = Path.Combine(App.ApplicationDataPath, "Playlists");
            if (!Directory.Exists(playlistPath))
            {
                Directory.CreateDirectory(playlistPath);
            }

            foreach (var file in Directory.EnumerateFiles(playlistPath, "*.json"))
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
                CurrentTrackIndex = random.Next(0, currentlyPlayedPlaylist?.Tracks?.Count - 1 ?? 0);
            }
            else if (CurrentTrackIndex > 0)
            {
                CurrentTrackIndex--;
            }
            else if (repeat)
            {
                CurrentTrackIndex = currentlyPlayedPlaylist?.Tracks?.Count - 1 ?? 0;
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
