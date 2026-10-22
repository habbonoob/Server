using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Habbo.Relay;

/// <summary>
/// The packets the client sends here, written as Habbo.PacketLogger writes its captures: a pair of files named after
/// the capture's start (and name, if given), <c>.txt</c> in G-Earth's export format and <c>.jsonl</c> with one packet
/// (<c>seq</c>, <c>time</c>, <c>dir</c>, <c>id</c>, <c>name</c>, <c>length</c>, <c>hex</c>) or marker per line.
/// </summary>
public sealed class CaptureLog : IDisposable
{
    private static readonly JsonSerializerOptions Json = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly Lock _lock = new();
    private readonly string _directory;
    private readonly ILogger<CaptureLog> _logger;
    private StreamWriter? _text;
    private StreamWriter? _jsonl;
    private long _seq;
    private long _written;

    public CaptureLog(IOptions<RelayOptions> options, ILogger<CaptureLog> logger)
    {
        _logger = logger;
        _directory = Resolve(options.Value.CaptureDirectory);
    }

    public string? CapturePath { get; private set; }

    public string NewCapture(string? name)
    {
        lock (_lock)
        {
            Close();
            var stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var file = string.IsNullOrWhiteSpace(name) ? stamp : $"{stamp}_{Sanitize(name)}";
            CapturePath = Unused(Path.Combine(_directory, file));
            _written = 0;
            _logger.LogInformation("Capturing to {Path}.", CapturePath);
            return CapturePath;
        }
    }

    public int Add(IReadOnlyList<CapturedPacket> packets)
    {
        lock (_lock)
        {
            EnsureOpen();
            var added = 0;
            foreach (var packet in packets)
            {
                byte[] body;
                try
                {
                    body = Convert.FromHexString(packet.Hex);
                }
                catch (FormatException)
                {
                    continue;
                }

                var incoming = packet.Dir == "in";
                _text!.Write(PacketText.Block(incoming, packet.Id, packet.Name, body));
                _jsonl!.WriteLine(JsonSerializer.Serialize(new
                {
                    seq = ++_seq,
                    time = DateTimeOffset.FromUnixTimeMilliseconds(packet.Time),
                    dir = incoming ? "in" : "out",
                    id = packet.Id,
                    name = packet.Name,
                    length = body.Length,
                    hex = Convert.ToHexString(body),
                }, Json));
                added++;
            }
            _written += added;
            _text!.Flush();
            _jsonl!.Flush();
            return added;
        }
    }

    /// <summary>Writes a section header, so a capture can be split into the steps that produced it.</summary>
    public void Mark(string label)
    {
        lock (_lock)
        {
            EnsureOpen();
            _text!.Write($"\n==================== {label} ====================\n\n");
            _jsonl!.WriteLine(JsonSerializer.Serialize(new { mark = label, time = DateTimeOffset.Now, afterSeq = _seq }, Json));
            _text.Flush();
            _jsonl.Flush();
        }
    }

    public object Status()
    {
        lock (_lock)
            return new { capture = CapturePath, packetsWritten = _written, lastSeq = _seq };
    }

    private void EnsureOpen()
    {
        if (_text is not null)
            return;
        CapturePath ??= NewCapture(null);
        Directory.CreateDirectory(_directory);
        _text = new StreamWriter(CapturePath + ".txt", append: true, new UTF8Encoding(false));
        _jsonl = new StreamWriter(CapturePath + ".jsonl", append: true, new UTF8Encoding(false));
    }

    private void Close()
    {
        _text?.Dispose();
        _jsonl?.Dispose();
        _text = null;
        _jsonl = null;
    }

    /// <summary>The path, or the path with -2, -3, … when a capture of that name exists already.</summary>
    private static string Unused(string path)
    {
        var candidate = path;
        for (var n = 2; File.Exists(candidate + ".txt") || File.Exists(candidate + ".jsonl"); n++)
            candidate = $"{path}-{n}";
        return candidate;
    }

    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Trim().Select(c => invalid.Contains(c) || c == ' ' ? '-' : c).ToArray());
    }

    /// <summary>An absolute path as is; a relative one under the repository root (the folder above the binary that
    /// has Habbo.slnx), so captures land beside the logger's.</summary>
    private static string Resolve(string configured)
    {
        if (Path.IsPathRooted(configured))
            return configured;
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Habbo.slnx")))
                return Path.Combine(directory.FullName, configured);
        return Path.Combine(AppContext.BaseDirectory, configured);
    }

    public void Dispose()
    {
        lock (_lock)
            Close();
    }
}
