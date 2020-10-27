using System;
using System.Buffers;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AudioSensei.Configuration;
using AudioSensei.Crypto;
using AudioSensei.Models;
using Avalonia.Threading;
using JetBrains.Annotations;
using ReactiveUI;
using Serilog;

namespace AudioSensei.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, IDisposable
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
        public double Total => AudioStream == null ? 0 : (AudioStream.CurrentTime / AudioStream.TotalTime * 100);

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

        private bool _disposed;

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
        private readonly ulong _typeHash;
        private TcpListener _playbackServer;
        private readonly object _playbackServerLock = new object();

        public MainWindowViewModel([NotNull] IAudioBackend audioBackend, [NotNull] PlayerConfiguration playerConfiguration)
        {
            _typeHash = FowlerNollVo1A.GetHash(GetType().FullName);

            Program.Exit += Dispose;

            AudioBackend = audioBackend;
            AudioBackend.Volume = playerConfiguration.DefaultVolume;
            
            YoutubePlayer = new YoutubePlayer(audioBackend);

            InitializeCommands();
            LoadPlaylists();

            timer.Tick += Tick;

            ProcessStartupData();
        }

        private void Free()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            AudioStream?.Dispose();
            AudioStream = null;
            AudioBackend.Dispose();
        }

        public void Dispose()
        {
            Free();
            GC.SuppressFinalize(this);
        }

        ~MainWindowViewModel()
        {
            Free();
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
                    Play(Track.CreateFromFile(s));
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
                    IPEndPoint endPoint;
                    lock (_playbackServerLock)
                    {
                        _playbackServer = new TcpListener(IPAddress.Loopback, 0);
                        _playbackServer.Start();
                        endPoint = (IPEndPoint)_playbackServer.LocalEndpoint;
                    }
                    ProcessTcp();
                    File.WriteAllText(lockPath, endPoint.Port.ToString());
                    Log.Information($"Listening to playback requests on {endPoint}");

                    Program.Exit += () =>
                    {
                        lock (mutexLock)
                        {
                            if (mutexStatus == null)
                            {
                                return;
                            }

                            if (!mutexStatus.Value)
                            {
                                mutexStatus.Value = tcpMutex.WaitOne();
                            }

                            Log.Information("Shutting down the playback server");
                            File.Delete(lockPath);
                            lock (_playbackServerLock)
                            {
                                _playbackServer.Stop();
                                _playbackServer = null;
                            }

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

        private bool SendPlaybackRequest(ushort port, [NotNull] string[] paths)
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

                        Span<byte> ulongSpan = stackalloc byte[sizeof(ulong)];
                        var typeHash = _typeHash;
                        MemoryMarshal.Write(ulongSpan, ref typeHash);
                        stream.Write(ulongSpan);

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
                            var s = paths[i];
                            var path = File.Exists(s) ? Path.GetFullPath(s) : s;
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

        private async void ProcessTcp()
        {
            while (true)
            {
                lock (_playbackServerLock)
                {
                    if (_playbackServer == null)
                    {
                        break;
                    }
                }

                try
                {
                    // ReSharper disable once InconsistentlySynchronizedField
                    var client = await _playbackServer.AcceptTcpClientAsync();
                    Log.Information($"Connection from {client.Client.RemoteEndPoint} received!");
                    _ = Task.Run(async () => await ProcessClient(client));
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Connection setup failed");
                }

                await Task.Delay(10);
            }
            Log.Information("Stopping listening to playback requests");
        }

        [NotNull]
        private async Task ProcessClient([NotNull] TcpClient client)
        {
            byte[] tempBuffer = null;
            try
            {
                await using (var stream = client.GetStream())
                {
                    tempBuffer = ArrayPool<byte>.Shared.Rent(sizeof(ulong) + sizeof(int) + sizeof(Source));
                    var ulongBuffer = new Memory<byte>(tempBuffer, 0, sizeof(ulong));
                    var intBuffer = new Memory<byte>(tempBuffer, sizeof(ulong), sizeof(int));
                    var sourceBuffer = new Memory<byte>(tempBuffer, sizeof(ulong) + sizeof(int), sizeof(Source));

                    int hashLenght;
                    var valueTask = stream.ReadAsync(ulongBuffer);
                    if (valueTask.IsCompleted)
                    {
                        hashLenght = valueTask.Result;
                    }
                    else
                    {
                        var task = valueTask.AsTask();
                        await Task.WhenAny(task, Task.Delay(10000));
                        if (!task.IsCompleted)
                        {
                            Log.Information("Playback server connecton terminated due to no identification data sent");
                            return;
                        }

                        hashLenght = task.Result;
                    }

                    var typeHash = MemoryMarshal.Read<ulong>(ulongBuffer.Span);
                    if (hashLenght != sizeof(ulong) || typeHash != _typeHash)
                    {
                        Log.Information($"Rejecting playback request containing invalid identification data, possible connection from other software (local: {_typeHash}, remote {typeHash})");
                        return;
                    }

                    await stream.ReadAsync(intBuffer);
                    var version = MemoryMarshal.Read<int>(intBuffer.Span);
                    if (version != ProtocolVersion)
                    {
                        Log.Information($"Rejecting playback request with a different protocol version (local: {ProtocolVersion}, remote {version})");
                        return;
                    }

                    App.MainWindow.SetForegroundWindow();

                    await stream.ReadAsync(intBuffer);
                    var pathCount = MemoryMarshal.Read<int>(intBuffer.Span);
                    Log.Information($"Reading {pathCount} playback requests");

                    for (int i = 0; i < pathCount; i++)
                    {
                        await stream.ReadAsync(intBuffer);
                        var length = MemoryMarshal.Read<int>(intBuffer.Span);
                        var buffer = ArrayPool<byte>.Shared.Rent(length);
                        await stream.ReadAsync(new Memory<byte>(buffer, 0, length));
                        var path = Encoding.UTF8.GetString(buffer, 0, length);
                        ArrayPool<byte>.Shared.Return(buffer);
                        await stream.ReadAsync(sourceBuffer);
                        var source = MemoryMarshal.Read<Source>(sourceBuffer.Span);

                        Log.Information($"Received a playback request for {path} from {source}");
                        switch (source)
                        {
                            // TODO: ass the path into some list
                            case Source.File when File.Exists(path):
                                await Play(Track.CreateFromFile(path));
                                break;
                            case Source.YouTube:
                                await Play(new Track(source, path));
                                break;
                            default:
                                Log.Warning($"Received invalid source ({source}) for playback from {path}");
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Receiving playback request failed");
            }
            finally
            {
                if (tempBuffer != null)
                {
                    ArrayPool<byte>.Shared.Return(tempBuffer);
                }
                client.Dispose();
            }
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
                var playlist = Playlist.Load(file);

                for (int i = 0; i < playlist.Tracks.Count; i++)
                {
                    var track = playlist.Tracks[i];

                    switch (track.Source)
                    {
                        case Source.File:
                            track.LoadMetadataFromFile();
                            break;
                        case Source.YouTube:
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

        [NotNull]
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

            if (AudioStream != null && currentlyPlayedPlaylist == currentlyVisiblePlaylist && CurrentTrackIndex == SelectedTrackIndex)
            {
                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (AudioStream.Status)
                {
                    case AudioStreamStatus.Playing:
                        AudioStream.Pause();
                        if (Dispatcher.UIThread.CheckAccess())
                        {
                            timer.Stop();
                        }
                        else
                        {
                            await Dispatcher.UIThread.InvokeAsync(timer.Stop);
                        }
                        return;
                    case AudioStreamStatus.Paused:
                        AudioStream.Resume();
                        if (Dispatcher.UIThread.CheckAccess())
                        {
                            timer.Start();
                        }
                        else
                        {
                            await Dispatcher.UIThread.InvokeAsync(timer.Start);
                        }
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

            var track = currentlyPlayedPlaylist?.Tracks?[CurrentTrackIndex];

            if (track == null)
                throw new ArgumentNullException(nameof(track));

            await Play(track.Value);
        }

        private void Stop()
        {
            AudioStream?.Dispose();
            AudioStream = null;
            if (Dispatcher.UIThread.CheckAccess())
            {
                timer.Stop();
            }
            else
            {
                Dispatcher.UIThread.Post(timer.Stop);
            }
        }

        [NotNull]
        private async Task Previous(bool repeat = true, bool shuffle = false)
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

            var track = currentlyPlayedPlaylist?.Tracks?[CurrentTrackIndex];

            if (track == null)
                throw new ArgumentNullException(nameof(track));

            await Play(track.Value);
        }

        [NotNull]
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

        [NotNull]
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

        [NotNull]
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

        [NotNull]
        private void CreatePlaylist()
        {
            if (!string.IsNullOrWhiteSpace(playlistName))
            {
                Playlists.Add(new Playlist(playlistName, Guid.NewGuid(), playlistAuthor, playlistDescription, new ObservableCollection<Track>()));
            }

            CancelPlaylistCreation();
        }

        [NotNull]
        private void CancelPlaylistCreation()
        {
            IsPlaylistCreatorVisible = false;
            PlaylistName = "";
            PlaylistAuthor = "";
            PlaylistDescription = "";
        }

        [NotNull]
        private void SelectPlaylist(Guid uniqueId)
        {
            CurrentlyVisiblePlaylist = Playlists.First(playlist => playlist.UniqueId == uniqueId);
        }

        [NotNull]
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

            if (Dispatcher.UIThread.CheckAccess())
            {
                timer.Start();
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(timer.Start);
            }
        }

        private async void Tick(object sender, EventArgs args)
        {
            this.RaisePropertyChanged(nameof(TotalTimeFormatted));
            this.RaisePropertyChanged(nameof(CurrentTimeFormatted));
            this.RaisePropertyChanged(nameof(Total));

            if (AudioStream == null || AudioStream.Status != AudioStreamStatus.Playing)
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
