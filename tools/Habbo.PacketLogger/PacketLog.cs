using System.Text;
using System.Text.Json;
using Xabbo;

namespace Habbo.PacketLogger;

/// <summary>
/// The capture on disk plus the last few thousand packets in memory. Each capture is a pair of
/// files: <c>.txt</c> in G-Earth's export format for the existing tools, and <c>.jsonl</c> with one
/// packet (or marker) per line, which is the one to query.
/// </summary>
/// <remarks>
/// Packets arrive on the extension's processing loop and commands on the HTTP threads, so every
/// member takes the same lock.
/// </remarks>
public sealed class PacketLog(string directory, int recentCapacity) : IDisposable
{
    static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>How often what is buffered goes to disk: a capture is readable while it runs, and stopping the
    /// extension loses at most this much.</summary>
    static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(1);

    readonly object _lock = new();
    readonly LinkedList<LoggedPacket> _recent = new();
    Timer? _flusher;

    StreamWriter? _text;
    StreamWriter? _jsonl;
    long _seq;
    long _written;
    bool _dirty;

    /// <summary>Completed by the next packet, for waiters to wake on instead of polling.</summary>
    TaskCompletionSource _added = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ClientType Client { get; set; } = ClientType.Flash;
    public bool Recording { get; private set; } = true;
    public string? CapturePath { get; private set; }

    public long LastSeq { get { lock (_lock) return _seq; } }

    /// <summary>A task the next packet completes. Take it before looking, so a packet in between still wakes you.</summary>
    public Task NextPacket { get { lock (_lock) return _added.Task; } }

    /// <summary>Starts writing the buffers to disk every <see cref="FlushInterval"/>.</summary>
    public void StartFlushing() => _flusher ??= new Timer(_ => FlushIfDirty(), null, FlushInterval, FlushInterval);

    /// <summary>Starts a new pair of capture files; the next packet opens them.</summary>
    public string NewCapture(string? name = null)
    {
        lock (_lock)
        {
            Close();
            var stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var file = string.IsNullOrWhiteSpace(name) ? stamp : $"{stamp}_{Sanitize(name)}";
            CapturePath = Unused(Path.Combine(directory, file));
            _written = 0;
            return CapturePath;
        }
    }

    public void SetRecording(bool recording)
    {
        lock (_lock)
        {
            if (Recording == recording) return;
            Recording = recording;
            WriteMarkerLocked(recording ? "recording resumed" : "recording paused");
        }
    }

    /// <summary>Writes a section header, so a capture can be split into the steps that produced it.</summary>
    public void Mark(string label)
    {
        lock (_lock) WriteMarkerLocked(label);
    }

    public LoggedPacket Add(DateTimeOffset time, Direction direction, short id, string? name, byte[] body, bool blocked)
    {
        lock (_lock)
        {
            var packet = new LoggedPacket(++_seq, time, direction, id, name, body, blocked);

            _recent.AddLast(packet);
            while (_recent.Count > recentCapacity) _recent.RemoveFirst();

            if (Recording)
            {
                EnsureOpen();
                _text!.Write(PacketFormat.ToGEarthBlock(packet, Client));
                _jsonl!.WriteLine(JsonSerializer.Serialize(ToJson(packet, includeText: false), Json));
                _written++;
                _dirty = true;
            }

            var added = _added;
            _added = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            added.SetResult();
            return packet;
        }
    }

    /// <summary>
    /// Recent packets after <paramref name="since"/> that pass <paramref name="filter"/>, oldest first: the first
    /// <paramref name="limit"/> of them, so paging on with the last seq misses none, or with <paramref name="newest"/>
    /// the last <paramref name="limit"/>.
    /// </summary>
    /// <remarks>
    /// Only the packets after <paramref name="since"/> are copied under the lock; the filter, whose regexes come from
    /// whoever called, runs outside it, so a slow one cannot hold up the packets being intercepted.
    /// </remarks>
    public List<LoggedPacket> Recent(long since, int limit, Func<LoggedPacket, bool> filter, bool newest = false)
    {
        var after = new List<LoggedPacket>();
        lock (_lock)
        {
            for (var node = _recent.Last; node is not null && node.Value.Seq > since; node = node.Previous)
                after.Add(node.Value);
        }

        after.Reverse();
        var matches = after.Where(filter);
        return newest ? matches.TakeLast(limit).ToList() : matches.Take(limit).ToList();
    }

    public object Status()
    {
        lock (_lock)
        {
            return new
            {
                recording = Recording,
                capture = CapturePath,
                packetsWritten = _written,
                lastSeq = _seq,
                buffered = _recent.Count,
            };
        }
    }

    public void Flush()
    {
        lock (_lock)
        {
            _text?.Flush();
            _jsonl?.Flush();
            _dirty = false;
        }
    }

    void FlushIfDirty()
    {
        lock (_lock)
        {
            if (!_dirty) return;
            _text?.Flush();
            _jsonl?.Flush();
            _dirty = false;
        }
    }

    public object ToJson(LoggedPacket p, bool includeText) => new
    {
        seq = p.Seq,
        time = p.Time,
        dir = p.Direction == Direction.In ? "in" : "out",
        id = p.Id,
        name = p.Name,
        length = p.Body.Length,
        blocked = p.Blocked ? (bool?)true : null,
        hex = Convert.ToHexString(p.Body),
        text = includeText ? PacketFormat.Escape(p.Body) : null,
    };

    void WriteMarkerLocked(string label)
    {
        EnsureOpen();
        _text!.Write($"\n==================== {label} ====================\n\n");
        _jsonl!.WriteLine(JsonSerializer.Serialize(new { mark = label, time = DateTimeOffset.Now, afterSeq = _seq }, Json));
        _text.Flush();
        _jsonl.Flush();
    }

    void EnsureOpen()
    {
        if (_text is not null) return;
        CapturePath ??= NewCapture();
        Directory.CreateDirectory(directory);
        _text = new StreamWriter(CapturePath + ".txt", append: true, new UTF8Encoding(false));
        _jsonl = new StreamWriter(CapturePath + ".jsonl", append: true, new UTF8Encoding(false));
    }

    void Close()
    {
        _text?.Dispose();
        _jsonl?.Dispose();
        _text = null;
        _jsonl = null;
    }

    /// <summary>The path, or the path with -2, -3, … when a capture of that name exists already (two in one second).</summary>
    static string Unused(string path)
    {
        var candidate = path;
        for (var n = 2; File.Exists(candidate + ".txt") || File.Exists(candidate + ".jsonl"); n++)
            candidate = $"{path}-{n}";
        return candidate;
    }

    static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Trim().Select(c => invalid.Contains(c) || c == ' ' ? '-' : c).ToArray());
    }

    public void Dispose()
    {
        _flusher?.Dispose();
        lock (_lock) Close();
    }
}
