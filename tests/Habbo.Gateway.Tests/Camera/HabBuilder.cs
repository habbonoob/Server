using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Habbo.Gateway.Tests.Camera;

/// <summary>Writes a .hab bundle as the web client's tooling does: the header, the deflated JSON index, the entries
/// stored as they are.</summary>
internal static class HabBuilder
{
    public static byte[] Build(string name, params (string Name, string MimeType, byte[] Bytes)[] entries)
    {
        var payload = new MemoryStream();
        var index = new List<object>();
        foreach (var (entryName, mimeType, bytes) in entries)
        {
            index.Add(new { name = entryName, mimeType, offset = payload.Length, storedLength = bytes.Length, originalLength = bytes.Length, compression = "none" });
            payload.Write(bytes);
        }

        var json = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { format = "hab", version = 1, name, entries = index }));
        var deflated = new MemoryStream();
        using (var zlib = new ZLibStream(deflated, CompressionLevel.Optimal, leaveOpen: true))
            zlib.Write(json);

        var header = new byte[20];
        "HAB\0"u8.CopyTo(header);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(4), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(6), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(8), (uint)deflated.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(12), (uint)json.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(16), (uint)payload.Length);
        return [.. header, .. deflated.ToArray(), .. payload.ToArray()];
    }

    public static byte[] Json(object value) => JsonSerializer.SerializeToUtf8Bytes(value);
}
