using System.Buffers.Binary;
using System.IO.Compression;
using System.Text.Json;

namespace Habbo.Camera;

/// <summary>A .hab asset bundle, the web client's form of a Flash asset library, as the runtime reads it
/// (habbo-pixi's hab.ts): "HAB\0", version u16 (1), flags u16 (1), index length u32 (deflated), index length u32
/// (inflated), payload length u32; the deflated JSON index; the payload, whose entries it places.</summary>
public sealed class HabFile
{
    private const int HeaderLength = 20;
    private const int Version = 1;
    private const int Flags = 1;
    private const int MaxIndexLength = 64 * 1024 * 1024;

    private HabFile(string? name, IReadOnlyDictionary<string, HabEntry> entries)
    {
        Name = name;
        Entries = entries;
    }

    public string? Name { get; }

    public IReadOnlyDictionary<string, HabEntry> Entries { get; }

    public static HabFile Read(byte[] data)
    {
        if (data.Length < HeaderLength || data[0] != 'H' || data[1] != 'A' || data[2] != 'B' || data[3] != 0)
            throw new InvalidDataException("Not a HAB bundle.");
        var header = data.AsSpan();
        var version = BinaryPrimitives.ReadUInt16LittleEndian(header[4..]);
        var flags = BinaryPrimitives.ReadUInt16LittleEndian(header[6..]);
        var storedIndex = BinaryPrimitives.ReadUInt32LittleEndian(header[8..]);
        var indexLength = BinaryPrimitives.ReadUInt32LittleEndian(header[12..]);
        var payloadLength = BinaryPrimitives.ReadUInt32LittleEndian(header[16..]);
        if (version != Version || flags != Flags)
            throw new InvalidDataException($"Unsupported HAB bundle version {version}, flags {flags}.");
        if (indexLength > MaxIndexLength || HeaderLength + (long)storedIndex + payloadLength != data.Length)
            throw new InvalidDataException("The HAB bundle's lengths do not match its size.");

        var payloadStart = HeaderLength + (int)storedIndex;
        var index = Inflate(data.AsSpan(HeaderLength, (int)storedIndex));
        using var json = JsonDocument.Parse(index);
        var root = json.RootElement;
        var entries = new Dictionary<string, HabEntry>(StringComparer.Ordinal);
        foreach (var entry in root.GetProperty("entries").EnumerateArray())
        {
            var name = entry.GetProperty("name").GetString()!;
            var offset = entry.GetProperty("offset").GetInt64();
            var stored = entry.GetProperty("storedLength").GetInt64();
            if (offset < 0 || stored < 0 || offset + stored > payloadLength)
                throw new InvalidDataException($"HAB entry {name} is outside the payload.");
            var bytes = data.AsSpan(payloadStart + (int)offset, (int)stored);
            entries[name] = new HabEntry(name, entry.GetProperty("mimeType").GetString() ?? "",
                entry.GetProperty("compression").GetString() == "deflate" ? Inflate(bytes) : bytes.ToArray());
        }
        return new HabFile(root.TryGetProperty("name", out var n) ? n.GetString() : null, entries);
    }

    /// <summary>zlib-wrapped deflate (78 xx), or a raw deflate stream.</summary>
    private static byte[] Inflate(ReadOnlySpan<byte> data)
    {
        var zlib = data.Length >= 2 && (data[0] & 0x0f) == 8 && ((data[0] << 8) | data[1]) % 31 == 0;
        using var input = new MemoryStream(data.ToArray());
        using Stream inflater = zlib ? new ZLibStream(input, CompressionMode.Decompress) : new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        inflater.CopyTo(output);
        return output.ToArray();
    }
}
