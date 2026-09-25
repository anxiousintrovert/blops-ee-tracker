namespace EETracker.Core;

public interface ITelemetrySource : IAsyncDisposable
{
    event Action<TelemetryEvent>? EventReceived;
    Task StartAsync(CancellationToken cancellationToken);
}

public sealed class JsonlReplaySource(string path, TimeSpan? delay = null) : ITelemetrySource
{
    public event Action<TelemetryEvent>? EventReceived;
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await foreach (var line in File.ReadLinesAsync(path, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line)) continue;
            var item = TelemetryJson.Parse(line);
            if (item is null) continue;
            EventReceived?.Invoke(item);
            if (delay is { } d && d > TimeSpan.Zero) await Task.Delay(d, cancellationToken);
        }
    }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

/// <summary>Follows complete JSONL records across appends, truncation, and map-session replacement.</summary>
public sealed class JsonlTailSource(string path, TimeSpan? pollInterval = null, bool readExisting = false, TimeSpan? idleTimeout = null) : ITelemetrySource
{
    public event Action<TelemetryEvent>? EventReceived;
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var interval = pollInterval ?? TimeSpan.FromMilliseconds(250);
        string? firstLine = null;
        long offset = 0;
        var pending = new List<byte>();
        var opened = false;
        var sawHeartbeat = false;
        var disconnected = false;
        DateTimeOffset lastHeartbeat = DateTimeOffset.MinValue;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (File.Exists(path))
                {
                    await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    var marker = await ReadFirstLineAsync(stream, cancellationToken);
                    var initialRead = !opened;
                    if (!opened)
                    {
                        opened = true;
                        firstLine = marker;
                        if (!readExisting) offset = stream.Length;
                    }
                    else if (marker != firstLine || stream.Length < offset)
                    {
                        firstLine = marker;
                        offset = 0;
                        pending.Clear();
                        sawHeartbeat = false;
                    }

                    stream.Seek(offset, SeekOrigin.Begin);
                    var buffer = new byte[4096];
                    int count;
                    while ((count = await stream.ReadAsync(buffer, cancellationToken)) > 0)
                    {
                        offset += count;
                        for (var i = 0; i < count; i++)
                        {
                            if (buffer[i] != (byte)'\n') { pending.Add(buffer[i]); continue; }
                            var line = System.Text.Encoding.UTF8.GetString(pending.ToArray()).TrimEnd('\r');
                            pending.Clear();
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            var item = TelemetryJson.Parse(line);
                            if (item is null) continue;
                            if (disconnected && item.Type != "session_ended")
                            {
                                EventReceived?.Invoke(Status("connected"));
                                disconnected = false;
                            }
                            EventReceived?.Invoke(item);
                            if (item.Type == "heartbeat")
                            {
                                sawHeartbeat = true;
                                lastHeartbeat = initialRead && readExisting
                                    ? new DateTimeOffset(File.GetLastWriteTimeUtc(path), TimeSpan.Zero)
                                    : DateTimeOffset.UtcNow;
                            }
                            // A match ending is not a transport ending: keep following the log for the next match.
                        }
                    }
                    if (sawHeartbeat && idleTimeout is { } timeout && DateTimeOffset.UtcNow - lastHeartbeat > timeout && !disconnected)
                    {
                        EventReceived?.Invoke(Status("disconnected"));
                        disconnected = true;
                    }
                }
                else if (opened && !disconnected)
                {
                    EventReceived?.Invoke(Status("disconnected"));
                    disconnected = true;
                    opened = false;
                    firstLine = null;
                    offset = 0;
                    pending.Clear();
                }
            }
            catch (IOException) { /* A writer may be replacing the file; retry on the next poll. */ }
            await Task.Delay(interval, cancellationToken);
        }
    }
    private static TelemetryEvent Status(string value) => new()
    {
        TimestampUtc = DateTimeOffset.UtcNow, SessionId = "file-transport", Type = "transport_status", SignalValue = value, Source = "file-transport"
    };

    private static async Task<string?> ReadFirstLineAsync(FileStream stream, CancellationToken cancellationToken)
    {
        stream.Seek(0, SeekOrigin.Begin);
        var bytes = new List<byte>();
        var one = new byte[1];
        while (bytes.Count < 1024 && await stream.ReadAsync(one, cancellationToken) == 1)
        {
            if (one[0] == (byte)'\n') return System.Text.Encoding.UTF8.GetString(bytes.ToArray()).TrimEnd('\r');
            bytes.Add(one[0]);
        }
        return null;
    }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

/// <summary>Listens to both game logs until the first match is identified, then follows that game's log.</summary>
public sealed class AutoGameTelemetrySource(string bo1Path, string bo2Path, TimeSpan? idleTimeout = null) : ITelemetrySource
{
    public event Action<TelemetryEvent>? EventReceived;
    private string? _lockedGame;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var t5 = Watch(bo1Path, "bo1", linked.Token);
        var t6 = Watch(bo2Path, "bo2", linked.Token);
        try { await Task.WhenAll(t5, t6); } finally { linked.Cancel(); }
    }

    private async Task Watch(string path, string game, CancellationToken token)
    {
        var source = new JsonlTailSource(path, idleTimeout: idleTimeout, readExisting: true);
        source.EventReceived += item =>
        {
            var detectedGame = item.Game ?? (game == "bo1" ? "bo1" : item.Map?.StartsWith("zm_", StringComparison.Ordinal) == true ? "bo2" : null);
            if (_lockedGame is null && detectedGame == game && (item.Type == "session_started" || !string.IsNullOrWhiteSpace(item.Map)))
                Interlocked.CompareExchange(ref _lockedGame, game, null);
            if (_lockedGame == game)
            {
                EventReceived?.Invoke(item with { Game = item.Game ?? game });
                if (item.Type == "session_ended") Interlocked.Exchange(ref _lockedGame, null);
            }
        };
        try { await source.StartAsync(token); } finally { await source.DisposeAsync(); }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
