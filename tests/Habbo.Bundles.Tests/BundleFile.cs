using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace Habbo.Bundles.Tests;

/// <summary>Reads a bundle back, as the client does (docs/asset-bundles.md).</summary>
internal sealed class BundleFile
{
    public required string Name { get; init; }
    public required List<Sprite> Sprites { get; init; }
    public required List<Document> Documents { get; init; }
    public required List<byte[]> Atlases { get; init; }

    public Sprite this[string name] => Sprites.Single(s => s.Name == name);

    public sealed record Sprite(string Name, int Atlas, int X, int Y, int Width, int Height, int OffsetX, int OffsetY, int Flags);

    public sealed record Document(string Name, BundleDocumentKind Kind, string Text);

    public static BundleFile Read(byte[] data)
    {
        var at = 0;
        Assert.Equal("HCB\0"u8.ToArray(), data[..4]);
        at = 4;
        Assert.Equal(BundleWriter.Version, U16());
        Assert.Equal(BundleWriter.DeflatedManifest, U16());
        var stored = (int)U32();
        var inflated = (int)U32();
        using var zlib = new ZLibStream(new MemoryStream(data, at, stored), CompressionMode.Decompress);
        var manifest = new MemoryStream();
        zlib.CopyTo(manifest);
        Assert.Equal(inflated, (int)manifest.Length);
        var rest = data[(at + stored)..];
        data = manifest.ToArray();
        at = 0;
        var name = Str();
        var sprites = new List<Sprite>();
        for (var count = U32(); count > 0; count--)
            sprites.Add(new Sprite(Str(), data[at++], U16(), U16(), U16(), U16(), I16(), I16(), data[at++]));
        var documents = new List<Document>();
        for (var count = U32(); count > 0; count--)
        {
            var docName = Str();
            var kind = (BundleDocumentKind)data[at++];
            var length = (int)U32();
            documents.Add(new Document(docName, kind, Encoding.UTF8.GetString(data, at, length)));
            at += length;
        }
        Assert.Equal(data.Length, at);
        data = rest;
        at = 0;
        var atlases = new List<byte[]>();
        for (var count = U16(); count > 0; count--)
        {
            var length = (int)U32();
            atlases.Add(data[at..(at + length)]);
            at += length;
        }
        Assert.Equal(data.Length, at);
        return new BundleFile { Name = name, Sprites = sprites, Documents = documents, Atlases = atlases };

        ushort U16() { var v = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(at)); at += 2; return v; }
        short I16() { var v = BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(at)); at += 2; return v; }
        uint U32() { var v = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(at)); at += 4; return v; }
        string Str() { var length = U16(); var s = Encoding.UTF8.GetString(data, at, length); at += length; return s; }
    }
}
