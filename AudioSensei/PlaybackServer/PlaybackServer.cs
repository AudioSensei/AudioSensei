using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AudioSensei.Crypto;
using AudioSensei.Models;
using AudioSensei.ViewModels;
using JetBrains.Annotations;
using Serilog;

namespace AudioSensei.PlaybackServer
{
    class PlaybackServer
    {
        private MainWindowViewModel mediaController;

        private const int ProtocolVersion = 1;
        private readonly ulong _typeHash;
        private TcpListener _playbackServer;
        private readonly object _playbackServerLock = new();

        public PlaybackServer()
        {
            _typeHash = FowlerNollVo1A.GetHash(GetType().FullName);
        }

        public bool TryConnectLocal(string[] cmd)
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
                    if (!(mutexStatus?.Value ?? false))
                        return;

                    tcpMutex?.ReleaseMutex();
                    mutexStatus.Value = false;
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
                    }

                    return true;
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

            return false;
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
                                await mediaController.Play(Track.CreateFromFile(path));
                                break;
                            case Source.YouTube:
                                await mediaController.Play(new Track(source, path));
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
    }
}
