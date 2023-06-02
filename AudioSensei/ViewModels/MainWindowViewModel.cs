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
using AudioSensei.Discord;
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
            get => _selectedPageIndex;
            set => this.RaiseAndSetIfChanged(ref _selectedPageIndex, value, nameof(SelectedPageIndex));
        }

        // Playlist creator
        public bool IsPlaylistCreatorVisible
        {
            get => _isPlaylistCreatorVisible;
            set => this.RaiseAndSetIfChanged(ref _isPlaylistCreatorVisible, value, nameof(IsPlaylistCreatorVisible));
        }
        public string PlaylistName
        {
            get => _playlistName;
            set => this.RaiseAndSetIfChanged(ref _playlistName, value, nameof(PlaylistName));
        }
        public string PlaylistDescription
        {
            get => _playlistDescription;
            set => this.RaiseAndSetIfChanged(ref _playlistDescription, value, nameof(PlaylistDescription));
        }
        public string PlaylistAuthor
        {
            get => _playlistAuthor;
            set => this.RaiseAndSetIfChanged(ref _playlistAuthor, value, nameof(PlaylistAuthor));
        }

        // Playlists
        public ObservableCollection<PlaylistViewModel> Playlists { get; set; } = new();

        public Playlist? CurrentlyPlayedPlaylist
        {
            get => _currentlyPlayedPlaylist;
            set => this.RaiseAndSetIfChanged(ref _currentlyPlayedPlaylist, value, nameof(CurrentlyPlayedPlaylist));
        }
        public Playlist CurrentlyVisiblePlaylist
        {
            get => _currentlyVisiblePlaylist;
            set
            {
                this.RaiseAndSetIfChanged(ref _currentlyVisiblePlaylist, value, nameof(CurrentlyVisiblePlaylist));

                if (_currentlyPlayedPlaylist == _currentlyVisiblePlaylist)
                {
                    SelectedTrackIndex = CurrentTrackIndex;
                }
            }
        }

        //Tracks
        public int SelectedTrackIndex
        {
            get => _selectedTrackIndex;
            set => this.RaiseAndSetIfChanged(ref _selectedTrackIndex, value, nameof(SelectedTrackIndex));
        }
        public int CurrentTrackIndex { get; set; } = -1;

        // Current Stream
        public string CurrentTimeFormatted => AudioStream == null ? "00:00" : AudioStream.CurrentTime.ToPlaybackPosition();
        public string TotalTimeFormatted => AudioStream == null ? "00:00" : AudioStream.TotalTime.ToPlaybackPosition();
        public double Total => AudioStream == null ? 0 : AudioStream.CurrentTime / AudioStream.TotalTime * 100;

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
        private readonly DiscordPresence _discordPresence;

        public IAudioStream AudioStream { get; private set; }

        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(100.0) };
        private readonly Random _random = new(RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue));

        private volatile int _disposed;
        private readonly object _disposeLock = new();

        private bool _repeat;
        private bool _shuffle;

        // Pages
        private int _selectedPageIndex;

        // Playlist creator
        private bool _isPlaylistCreatorVisible;
        private string _playlistName = "";
        private string _playlistAuthor = "";
        private string _playlistDescription = "";

        // Playlists
        private Playlist _currentlyVisiblePlaylist = new("", Guid.NewGuid(), "", "", new ObservableCollection<Track>());
        private Playlist? _currentlyPlayedPlaylist;

        // Tracks
        private int _selectedTrackIndex = -1;
        private Track _currentTrack;

        // IPC
        private const int ProtocolVersion = 1;
        private readonly ulong _typeHash;
        private TcpListener _playbackServer;
        private readonly object _playbackServerLock = new();

        // Status update
        private readonly Thread _statusThread;
        private AudioStreamStatus _lastStatus = AudioStreamStatus.Invalid;
        public event Action<AudioStreamStatus> StatusChanged;

        public MainWindowViewModel([NotNull] IAudioBackend audioBackend, [NotNull] PlayerConfiguration playerConfiguration)
        {
            _typeHash = FowlerNollVo1A.GetHash(GetType().FullName);

            Program.Exit += Dispose;

            AudioBackend = audioBackend;
            AudioBackend.Volume = playerConfiguration.DefaultVolume;

            YoutubePlayer = new YoutubePlayer(audioBackend);
            _discordPresence = new DiscordPresence("668517213388668939");

            InitializeCommands();
            LoadPlaylists();

            StatusChanged += status =>
            {
                var t = _currentTrack;
                switch (status)
                {
                    case AudioStreamStatus.Invalid:
                        _discordPresence.UpdatePresence(null, null, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), 0);
                        break;
                    case AudioStreamStatus.Paused:
                        _discordPresence.UpdatePresence($"Paused: {t.Name ?? "Unknown track"}", t.Author ?? "Unknown author", DateTimeOffset.UtcNow.ToUnixTimeSeconds(), 0);
                        break;
                    case AudioStreamStatus.Playing:
                        var startTimestamp = DateTimeOffset.UtcNow;
                        _discordPresence.UpdatePresence($"Playing: {t.Name ?? "Unknown track"}", t.Author ?? "Unknown author", startTimestamp.ToUnixTimeSeconds(), (startTimestamp + AudioStream.TotalTime - AudioStream.CurrentTime).ToUnixTimeSeconds());
                        break;
                }
            };
            _timer.Tick += Tick;

            _statusThread = new Thread(StatusChecker)
            { IsBackground = true, Priority = ThreadPriority.BelowNormal };
            _statusThread.Start();

            ProcessStartupData();
        }

        private void StatusChecker()
        {
            while (true)
            {
                lock (_disposeLock)
                {
                    if (_disposed != 0)
                        return;
                    var status = AudioStream?.Status ?? AudioStreamStatus.Invalid;
                    if (_lastStatus != status)
                    {
                        _lastStatus = status;
                        StatusChanged?.Invoke(status);
                    }
                }

                Thread.Sleep(150);
            }
        }

        private void Free()
        {
            lock (_disposeLock)
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                    return;
                _discordPresence.Dispose();
                AudioStream?.Dispose();
                AudioStream = null;
                AudioBackend.Dispose();
            }
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
            NextCommand = ReactiveCommand.CreateFromTask(async () => await Next(_repeat, _shuffle));
            PreviousCommand = ReactiveCommand.CreateFromTask(async () => await Previous(_repeat, _shuffle));
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

                foreach (var track in playlist.Tracks)
                {
                    Task.Run(async () =>
                    {
                        switch (track.Source)
                        {
                            case Source.File:
                                track.LoadMetadataFromFile();
                                break;
                            case Source.YouTube:
                                var info = await YoutubePlayer.GetInfo(track.Url);
                                track.Name = info.Video.Title;
                                track.Author = info.Video.Author.Title;
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    });
                }

                Playlists.Add(new PlaylistViewModel
                {
                    Playlist = playlist,
                    Command = SelectPlaylistCommand,
                });
            }

            if (Playlists.Count > 0)
            {
                CurrentlyVisiblePlaylist = Playlists[0].Playlist;
            }
        }

        private async Task PlayOrPause()
        {
            _currentlyPlayedPlaylist ??= _currentlyVisiblePlaylist;

            if (_currentlyPlayedPlaylist?.Tracks!.Count == 0)
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

                    if (_currentlyPlayedPlaylist == _currentlyVisiblePlaylist)
                    {
                        SelectedTrackIndex = 0;
                    }
                }
            }

            if (AudioStream != null && _currentlyPlayedPlaylist == _currentlyVisiblePlaylist && CurrentTrackIndex == SelectedTrackIndex)
            {
                var t = _currentTrack;
                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (AudioStream.Status)
                {
                    case AudioStreamStatus.Playing:
                        AudioStream.Pause();
                        _discordPresence.UpdatePresence($"Paused: {t.Name ?? "Unknown track"}", t.Author ?? "Unknown author", DateTimeOffset.UtcNow.ToUnixTimeSeconds(), 0);
                        if (Dispatcher.UIThread.CheckAccess())
                        {
                            _timer.Stop();
                        }
                        else
                        {
                            await Dispatcher.UIThread.InvokeAsync(_timer.Stop);
                        }
                        return;
                    case AudioStreamStatus.Paused:
                        AudioStream.Resume();
                        var startTimestamp = DateTimeOffset.UtcNow;
                        _discordPresence.UpdatePresence($"Playing: {t.Name ?? "Unknown track"}", t.Author ?? "Unknown author", startTimestamp.ToUnixTimeSeconds(), (startTimestamp + AudioStream.TotalTime - AudioStream.CurrentTime).ToUnixTimeSeconds());
                        if (Dispatcher.UIThread.CheckAccess())
                        {
                            _timer.Start();
                        }
                        else
                        {
                            await Dispatcher.UIThread.InvokeAsync(_timer.Start);
                        }
                        return;
                }
            }

            if (_currentlyPlayedPlaylist != _currentlyVisiblePlaylist)
            {
                _currentlyPlayedPlaylist = _currentlyVisiblePlaylist;

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

            await Play(_currentlyPlayedPlaylist?.Tracks?[CurrentTrackIndex]);
        }

        private void Stop()
        {
            AudioStream?.Dispose();
            AudioStream = null;
            _discordPresence.UpdatePresence(null, null, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), 0);
            if (Dispatcher.UIThread.CheckAccess())
            {
                _timer.Stop();
            }
            else
            {
                Dispatcher.UIThread.Post(_timer.Stop);
            }
        }

        private async Task Previous(bool repeat = true, bool shuffle = false)
        {
            if (_currentlyPlayedPlaylist == null)
            {
                return;
            }

            if (shuffle)
            {
                CurrentTrackIndex = _random.Next(0, _currentlyPlayedPlaylist?.Tracks?.Count - 1 ?? 0);
            }
            else if (CurrentTrackIndex > 0)
            {
                CurrentTrackIndex--;
            }
            else if (repeat)
            {
                CurrentTrackIndex = _currentlyPlayedPlaylist?.Tracks?.Count - 1 ?? 0;
            }
            else
            {
                return;
            }

            if (_currentlyPlayedPlaylist == _currentlyVisiblePlaylist)
            {
                SelectedTrackIndex = CurrentTrackIndex;
            }

            await Play(_currentlyPlayedPlaylist?.Tracks?[CurrentTrackIndex]);
        }

        private async Task Next(bool repeat = true, bool shuffle = false)
        {
            if (_currentlyPlayedPlaylist == null)
            {
                return;
            }

            if (shuffle)
            {
                CurrentTrackIndex = _random.Next(0, (_currentlyPlayedPlaylist?.Tracks?.Count ?? 0) - 1);
            }
            else if (CurrentTrackIndex < (_currentlyPlayedPlaylist?.Tracks?.Count ?? 0) - 1)
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

            if (_currentlyPlayedPlaylist == _currentlyVisiblePlaylist)
            {
                SelectedTrackIndex = CurrentTrackIndex;
            }

            await Play(_currentlyPlayedPlaylist?.Tracks?[CurrentTrackIndex]);
        }

        private void Shuffle()
        {
            if (_shuffle)
            {
                _shuffle = false;
            }
            else
            {
                _shuffle = true;
                _repeat = false;
            }
        }

        private void Repeat()
        {
            if (_repeat)
            {
                _repeat = false;
            }
            else
            {
                _repeat = true;
                _shuffle = false;
            }
        }

        private void CreatePlaylist()
        {
            if (!string.IsNullOrWhiteSpace(_playlistName))
            {
                var playlist = new Playlist(_playlistName, Guid.NewGuid(), _playlistAuthor, _playlistDescription, new ObservableCollection<Track>());

                Playlists.Add(new PlaylistViewModel
                {
                    Playlist = playlist,
                    Command = SelectPlaylistCommand,
                });
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
            CurrentlyVisiblePlaylist = Playlists.Select(playlist => playlist.Playlist).First(playlist => playlist.UniqueId == uniqueId);
        }

        private async Task Play(Track track)
        {
            if (track == null)
                throw new ArgumentNullException(nameof(track));

            AudioStream?.Dispose();
            AudioStream = null;

            AudioStream = track.Source switch
            {
                Source.File => AudioBackend.Play(new Uri(track.Url)),
                Source.YouTube => await YoutubePlayer.Play(track.Url),
                _ => throw new NotImplementedException()
            };

            _currentTrack = track;

            if (AudioStream == null)
            {
                return;
            }

            var startTimestamp = DateTimeOffset.UtcNow;
            _discordPresence.UpdatePresence($"Playing: {track.Name ?? "Unknown track"}", track.Author ?? "Unknown author", startTimestamp.ToUnixTimeSeconds(), (startTimestamp + AudioStream.TotalTime).ToUnixTimeSeconds());
            if (Dispatcher.UIThread.CheckAccess())
            {
                _timer.Start();
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(_timer.Start);
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
