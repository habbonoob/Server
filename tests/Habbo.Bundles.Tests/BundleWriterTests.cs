using SkiaSharp;

namespace Habbo.Bundles.Tests;

public class BundleWriterTests
{
    [Fact]
    public void TheAtlasHoldsEveryImageExactlyAndTheManifestSaysWhere()
    {
        using var library = new BundleLibrary("lib");
        // half-transparent pixels too: lossless WebP keeps them as they are
        library.Images.Add(new BundleImage("red", TestImages.Bitmap(3, 2, (x, y) => new SKColor(200, (byte)(x * 10), (byte)y, 128)), null, -4, 7, true, false, false));
        library.Images.Add(new BundleImage("blue", TestImages.Bitmap(1, 4, (_, y) => new SKColor(0, 0, 255, (byte)(255 - y))), null, 0, 0, false, false, false));
        library.Documents.Add(new BundleDocument("index", BundleDocumentKind.Xml, "<object type=\"lib\"/>"));

        var bundle = BundleFile.Read(BundleWriter.Write(library));

        Assert.Equal("lib", bundle.Name);
        var red = bundle["red"];
        Assert.Equal((3, 2, -4, 7, 4), (red.Width, red.Height, red.OffsetX, red.OffsetY, red.Flags));
        Assert.Equal(0, bundle["blue"].Flags);
        using var atlas = TestImages.DecodeExact(bundle.Atlases[red.Atlas]);
        Assert.Equal(new SKColor(200, 20, 1, 128), atlas.GetPixel(red.X + 2, red.Y + 1));
        var blue = bundle["blue"];
        Assert.Equal(new SKColor(0, 0, 255, 252), atlas.GetPixel(blue.X, blue.Y + 3));
        Assert.Equal(new BundleFile.Document("index", BundleDocumentKind.Xml, "<object type=\"lib\"/>"), Assert.Single(bundle.Documents));
    }

    [Fact]
    public void AnImageDrawnFromAnotherSharesItsRectangleWithItsOwnOffsetAndFlip()
    {
        using var library = new BundleLibrary("chair");
        library.Images.Add(new BundleImage("chair_64_a_0_0", TestImages.Bitmap(4, 4, (_, _) => SKColors.Green), null, 10, 20, true, false, false));
        library.Images.Add(new BundleImage("chair_64_a_6_0", null, "chair_64_a_0_0", 30, 20, true, true, false));
        // a chain is followed to the image with pixels
        library.Images.Add(new BundleImage("chair_64_a_4_0", null, "chair_64_a_6_0", 1, 2, true, false, false));
        library.Images.Add(new BundleImage("orphan", null, "nowhere", 0, 0, false, false, false));

        var bundle = BundleFile.Read(BundleWriter.Write(library));

        var drawn = bundle["chair_64_a_0_0"];
        var flipped = bundle["chair_64_a_6_0"];
        Assert.Equal((drawn.Atlas, drawn.X, drawn.Y, drawn.Width, drawn.Height), (flipped.Atlas, flipped.X, flipped.Y, flipped.Width, flipped.Height));
        Assert.Equal((30, 20, 1 | 4), (flipped.OffsetX, flipped.OffsetY, flipped.Flags));
        Assert.Equal(drawn.X, bundle["chair_64_a_4_0"].X);
        Assert.DoesNotContain(bundle.Sprites, s => s.Name == "orphan");
        Assert.Single(bundle.Atlases);
    }
}
