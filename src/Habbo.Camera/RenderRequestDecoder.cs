using System.IO.Compression;
using System.Text.Json;

namespace Habbo.Camera;

/// <summary>Reads RenderRoom's data: the room as the client composed it, as zlib-deflated JSON.</summary>
public static class RenderRequestDecoder
{
    /// <summary>The most the inflated JSON may be: a room's planes and sprites are a few hundred kilobytes; more is
    /// not a photo.</summary>
    public const int MaxJsonLength = 8 * 1024 * 1024;

    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = false };

    public static string Inflate(ReadOnlySpan<byte> deflated)
    {
        using var input = new MemoryStream(deflated.ToArray());
        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = zlib.Read(buffer)) > 0)
        {
            if (output.Length + read > MaxJsonLength)
                throw new InvalidDataException($"The render request inflates past {MaxJsonLength} bytes.");
            output.Write(buffer, 0, read);
        }
        return System.Text.Encoding.UTF8.GetString(output.GetBuffer(), 0, (int)output.Length);
    }

    public static RenderRequest Decode(ReadOnlySpan<byte> deflated) => Parse(Inflate(deflated));

    public static RenderRequest Parse(string json) =>
        JsonSerializer.Deserialize<RenderRequest>(json, Options) ?? throw new InvalidDataException("The render request is null.");
}
