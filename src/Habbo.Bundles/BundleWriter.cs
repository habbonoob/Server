using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using SkiaSharp;

namespace Habbo.Bundles;

/// <summary>Writes a library as a bundle (docs/asset-bundles.md): its images packed into lossless WebP atlases, and
/// a manifest of every sprite's rectangle and offset and of its documents. An image drawn from another becomes a
/// sprite over the other's rectangle, so the client never follows a source chain.</summary>
public static class BundleWriter
{
    public const ushort Version = 1;

    /** Bumped whenever the converter's output changes, so bundles kept from before are made again. */
    public const int ConverterRevision = 5;

    /// <summary>The header's flag for a manifest stored zlib-deflated: its documents are mostly text.</summary>
    public const ushort DeflatedManifest = 1;

    /// <summary>How deep a chain of sources is followed before it is taken for a loop.</summary>
    private const int MaxSourceDepth = 8;

    public static byte[] Write(BundleLibrary library, AtlasPacker? packer = null)
    {
        packer ??= new AtlasPacker();
        var byName = new Dictionary<string, BundleImage>(StringComparer.Ordinal);
        foreach (var image in library.Images)
            byName.TryAdd(image.Name, image);

        var drawn = library.Images.Where(i => i.Pixels is not null).ToList();
        var layout = packer.Pack(drawn.Select(i => (i.Pixels!.Width, i.Pixels!.Height)).ToList());
        var slots = new Dictionary<string, (int Page, int X, int Y, int W, int H)>(StringComparer.Ordinal);
        for (var i = 0; i < drawn.Count; i++)
        {
            var p = layout.Placements[i];
            slots[drawn[i].Name] = (p.Page, p.X, p.Y, drawn[i].Pixels!.Width, drawn[i].Pixels!.Height);
        }

        var sprites = new List<(BundleImage Image, (int Page, int X, int Y, int W, int H) Slot)>();
        foreach (var image in library.Images)
        {
            if (image.Pixels is not null)
                sprites.Add((image, slots[image.Name]));
            else if (Resolve(image, byName) is { } source && slots.TryGetValue(source.Name, out var slot))
                sprites.Add((image, slot));
        }

        using var output = new MemoryStream();
        output.Write("HCB\0"u8);
        WriteU16(output, Version);
        WriteU16(output, DeflatedManifest);
        var manifest = Manifest(library, sprites);
        var deflated = Deflate(manifest);
        WriteU32(output, (uint)deflated.Length);
        WriteU32(output, (uint)manifest.Length);
        output.Write(deflated);
        WriteU16(output, (ushort)layout.Pages.Count);
        for (var page = 0; page < layout.Pages.Count; page++)
        {
            var atlas = Atlas(layout.Pages[page], drawn.Where((_, i) => layout.Placements[i].Page == page).Select(i => (i.Pixels!, slots[i.Name])));
            WriteU32(output, (uint)atlas.Length);
            output.Write(atlas);
        }
        return output.ToArray();
    }

    private static BundleImage? Resolve(BundleImage image, Dictionary<string, BundleImage> byName)
    {
        var current = image;
        for (var depth = 0; depth < MaxSourceDepth && current.Pixels is null; depth++)
        {
            if (current.Source is null || !byName.TryGetValue(current.Source, out var next))
                return null;
            current = next;
        }
        return current.Pixels is null ? null : current;
    }

    private static byte[] Atlas((int Width, int Height) size, IEnumerable<(SKBitmap Pixels, (int Page, int X, int Y, int W, int H) Slot)> images)
    {
        using var atlas = new SKBitmap(new SKImageInfo(size.Width, size.Height, Bitmaps.ColorType, Bitmaps.AlphaType));
        atlas.Erase(SKColors.Transparent);
        foreach (var (pixels, slot) in images)
            Bitmaps.Blit(pixels, new SKRectI(0, 0, pixels.Width, pixels.Height), atlas, slot.X, slot.Y);
        using var pixmap = atlas.PeekPixels();
        using var data = pixmap.Encode(new SKWebpEncoderOptions(SKWebpEncoderCompression.Lossless, 100))
            ?? throw new InvalidOperationException("Skia could not encode the atlas as WebP.");
        return data.ToArray();
    }

    private static byte[] Manifest(BundleLibrary library, List<(BundleImage Image, (int Page, int X, int Y, int W, int H) Slot)> sprites)
    {
        using var manifest = new MemoryStream();
        WriteString(manifest, library.Name);
        WriteU32(manifest, (uint)sprites.Count);
        foreach (var (image, slot) in sprites)
        {
            WriteString(manifest, image.Name);
            manifest.WriteByte((byte)slot.Page);
            WriteU16(manifest, (ushort)slot.X);
            WriteU16(manifest, (ushort)slot.Y);
            WriteU16(manifest, (ushort)slot.W);
            WriteU16(manifest, (ushort)slot.H);
            WriteI16(manifest, (short)image.OffsetX);
            WriteI16(manifest, (short)image.OffsetY);
            manifest.WriteByte((byte)((image.FlipH ? 1 : 0) | (image.FlipV ? 2 : 0) | (image.HasOffset ? 4 : 0)));
        }
        WriteU32(manifest, (uint)library.Documents.Count);
        foreach (var document in library.Documents)
        {
            WriteString(manifest, document.Name);
            manifest.WriteByte((byte)document.Kind);
            var text = Encoding.UTF8.GetBytes(document.Text);
            WriteU32(manifest, (uint)text.Length);
            manifest.Write(text);
        }
        return manifest.ToArray();
    }

    private static byte[] Deflate(byte[] data)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
            zlib.Write(data);
        return output.ToArray();
    }

    private static void WriteString(Stream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        if (bytes.Length > ushort.MaxValue)
            throw new InvalidDataException($"A name of {bytes.Length} bytes is too long.");
        WriteU16(stream, (ushort)bytes.Length);
        stream.Write(bytes);
    }

    private static void WriteU16(Stream stream, ushort value)
    {
        Span<byte> b = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(b, value);
        stream.Write(b);
    }

    private static void WriteI16(Stream stream, short value)
    {
        Span<byte> b = stackalloc byte[2];
        BinaryPrimitives.WriteInt16LittleEndian(b, value);
        stream.Write(b);
    }

    private static void WriteU32(Stream stream, uint value)
    {
        Span<byte> b = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(b, value);
        stream.Write(b);
    }
}
