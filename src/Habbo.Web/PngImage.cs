using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace Habbo.Web;

/// <summary>An RGBA image read from and written to PNG: enough of the format for Habbo's image
/// parts (8-bit, non-interlaced greyscale, RGB, palette or RGBA) and for writing what the imager
/// draws.</summary>
public sealed class PngImage
{
    private const int BytesPerPixel = 4;
    private static readonly byte[] Signature = [137, 80, 78, 71, 13, 10, 26, 10];

    private const int Greyscale = 0;
    private const int TrueColour = 2;
    private const int Indexed = 3;
    private const int GreyscaleAlpha = 4;
    private const int TrueColourAlpha = 6;
    private const int SupportedBitDepth = 8;
    private const byte Opaque = byte.MaxValue;

    public PngImage(int width, int height)
    {
        Width = width;
        Height = height;
        Pixels = new byte[width * height * BytesPerPixel];
    }

    public int Width { get; }

    public int Height { get; }

    /// <summary>Row after row of R, G, B, A.</summary>
    public byte[] Pixels { get; }

    public (byte R, byte G, byte B, byte A) this[int x, int y]
    {
        get
        {
            var i = (y * Width + x) * BytesPerPixel;
            return (Pixels[i], Pixels[i + 1], Pixels[i + 2], Pixels[i + 3]);
        }
        set
        {
            var i = (y * Width + x) * BytesPerPixel;
            (Pixels[i], Pixels[i + 1], Pixels[i + 2], Pixels[i + 3]) = value;
        }
    }

    /// <summary>Reads a PNG, or null when it is one this reader does not handle.</summary>
    public static PngImage? Decode(byte[] png)
    {
        if (png.Length < Signature.Length || !png.AsSpan(0, Signature.Length).SequenceEqual(Signature))
            return null;

        int width = 0, height = 0, colourType = 0;
        byte[] palette = [];
        byte[] transparency = [];
        using var compressed = new MemoryStream();

        for (var offset = Signature.Length; offset + 8 <= png.Length;)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(offset));
            var type = Encoding.ASCII.GetString(png, offset + 4, 4);
            var data = png.AsSpan(offset + 8, length);
            switch (type)
            {
                case "IHDR":
                    width = BinaryPrimitives.ReadInt32BigEndian(data);
                    height = BinaryPrimitives.ReadInt32BigEndian(data[4..]);
                    colourType = data[9];
                    if (data[8] != SupportedBitDepth || data[12] != 0)
                        return null;
                    break;
                case "PLTE":
                    palette = data.ToArray();
                    break;
                case "tRNS":
                    transparency = data.ToArray();
                    break;
                case "IDAT":
                    compressed.Write(data);
                    break;
            }

            offset += length + 12;
        }

        var channels = colourType switch
        {
            Greyscale => 1,
            TrueColour => 3,
            Indexed => 1,
            GreyscaleAlpha => 2,
            TrueColourAlpha => 4,
            _ => 0,
        };
        if (channels == 0 || width <= 0 || height <= 0)
            return null;

        compressed.Position = 0;
        using var inflater = new ZLibStream(compressed, CompressionMode.Decompress);
        using var raw = new MemoryStream();
        inflater.CopyTo(raw);
        var scanlines = raw.ToArray();

        var stride = width * channels;
        var image = new PngImage(width, height);
        var previous = new byte[stride];
        var current = new byte[stride];
        for (var y = 0; y < height; y++)
        {
            var start = y * (stride + 1);
            if (start + stride + 1 > scanlines.Length)
                return null;

            var filter = scanlines[start];
            Array.Copy(scanlines, start + 1, current, 0, stride);
            Unfilter(filter, current, previous, channels);

            for (var x = 0; x < width; x++)
            {
                var p = x * channels;
                image[x, y] = colourType switch
                {
                    Greyscale => (current[p], current[p], current[p], Opaque),
                    GreyscaleAlpha => (current[p], current[p], current[p], current[p + 1]),
                    TrueColour => (current[p], current[p + 1], current[p + 2], Opaque),
                    Indexed => (
                        palette[current[p] * 3], palette[current[p] * 3 + 1], palette[current[p] * 3 + 2],
                        current[p] < transparency.Length ? transparency[current[p]] : Opaque),
                    _ => (current[p], current[p + 1], current[p + 2], current[p + 3]),
                };
            }

            (previous, current) = (current, previous);
        }

        return image;
    }

    private static void Unfilter(byte filter, byte[] line, byte[] previous, int bpp)
    {
        for (var i = 0; i < line.Length; i++)
        {
            int left = i >= bpp ? line[i - bpp] : 0;
            int up = previous[i];
            int upLeft = i >= bpp ? previous[i - bpp] : 0;
            line[i] = (byte)(line[i] + filter switch
            {
                1 => left,
                2 => up,
                3 => (left + up) / 2,
                4 => Paeth(left, up, upLeft),
                _ => 0,
            });
        }
    }

    private static int Paeth(int a, int b, int c)
    {
        var p = a + b - c;
        var (pa, pb, pc) = (Math.Abs(p - a), Math.Abs(p - b), Math.Abs(p - c));
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }

    /// <summary>Writes the image as an 8-bit RGBA PNG.</summary>
    public byte[] Encode()
    {
        using var raw = new MemoryStream();
        for (var y = 0; y < Height; y++)
        {
            raw.WriteByte(0);
            raw.Write(Pixels, y * Width * BytesPerPixel, Width * BytesPerPixel);
        }

        using var deflated = new MemoryStream();
        using (var deflater = new ZLibStream(deflated, CompressionLevel.Optimal, leaveOpen: true))
            deflater.Write(raw.ToArray());

        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, Width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), Height);
        header[8] = SupportedBitDepth;
        header[9] = TrueColourAlpha;

        using var png = new MemoryStream();
        png.Write(Signature);
        WriteChunk(png, "IHDR", header);
        WriteChunk(png, "IDAT", deflated.ToArray());
        WriteChunk(png, "IEND", []);
        return png.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        Span<byte> number = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(number, data.Length);
        stream.Write(number);

        var typeBytes = Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes);
        stream.Write(data);

        BinaryPrimitives.WriteUInt32BigEndian(number, Crc(Crc(uint.MaxValue, typeBytes), data) ^ uint.MaxValue);
        stream.Write(number);
    }

    /// <summary>PNG's CRC-32 (the zlib polynomial, reflected), run on from a previous value.</summary>
    private static uint Crc(uint crc, byte[] data)
    {
        foreach (var value in data)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ CrcPolynomial : crc >> 1;
        }

        return crc;
    }

    private const uint CrcPolynomial = 0xEDB88320;
}
