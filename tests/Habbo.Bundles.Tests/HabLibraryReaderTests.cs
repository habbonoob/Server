using Habbo.Camera;
using SkiaSharp;

namespace Habbo.Bundles.Tests;

public class HabLibraryReaderTests
{
    [Fact]
    public void ASheetLibraryCutsItsFramesAndKeepsItsJsonWithoutAssetsAndSheet()
    {
        // two frames side by side, keyed <documentClass>_<asset> as Habbo's sheets are
        var sheet = TestImages.Png(5, 2, (x, _) => x < 3 ? SKColors.Red : SKColors.Blue);
        var json = HabBuilder.Json(new
        {
            name = "stool",
            logicType = "furniture_basic",
            documentClass = "stool",
            assets = new Dictionary<string, object>
            {
                // a flip on an image with its own pixels is ignored, as Habbo ignores it
                ["stool_64_a_0_0"] = new { x = 12, y = -3, flipH = true },
                ["stool_64_a_2_0"] = new { source = "stool_64_a_0_0", x = 9, y = -3, flipH = true },
            },
            spritesheet = new
            {
                frames = new Dictionary<string, object>
                {
                    ["stool_stool_64_a_0_0"] = new { frame = new { x = 0, y = 0, w = 3, h = 2 }, rotated = false },
                    ["stool_stool_icon_a"] = new { frame = new { x = 3, y = 0, w = 2, h = 2 }, rotated = false },
                },
                meta = new { image = "stool.png" },
            },
        });
        var hab = HabFile.Read(HabBuilder.Build("stool", ("stool.json", "application/json", json), ("stool.png", "image/png", sheet)));

        using var library = HabLibraryReader.Read(hab, "fallback");

        Assert.Equal("stool", library.Name);
        var drawn = library.Images.Single(i => i.Name == "stool_64_a_0_0");
        Assert.Equal((3, 2, 12, -3, true, false), (drawn.Pixels!.Width, drawn.Pixels.Height, drawn.OffsetX, drawn.OffsetY, drawn.HasOffset, drawn.FlipH));
        Assert.Equal(SKColors.Red, drawn.Pixels.GetPixel(2, 1));
        var flipped = library.Images.Single(i => i.Name == "stool_64_a_2_0");
        Assert.Equal(("stool_64_a_0_0", true), (flipped.Source, flipped.FlipH));
        // a frame no asset names is an image still
        Assert.Equal(SKColors.Blue, library.Images.Single(i => i.Name == "stool_icon_a").Pixels!.GetPixel(0, 0));
        var document = Assert.Single(library.Documents);
        Assert.Equal(BundleDocumentKind.Json, document.Kind);
        Assert.Contains("furniture_basic", document.Text);
        Assert.DoesNotContain("spritesheet", document.Text);
        Assert.DoesNotContain("\"assets\"", document.Text);
        Assert.DoesNotContain("paletteAssets", document.Text);
    }

    [Fact]
    public void ATrimmedFrameIsTheImageItWasTrimmedFrom()
    {
        // a 2×2 red square trimmed out of a 5×6 image, from (3, 1)
        var sheet = TestImages.Png(2, 2, (_, _) => SKColors.Red);
        var json = HabBuilder.Json(new
        {
            name = "hair",
            documentClass = "hair",
            assets = new Dictionary<string, object> { ["h_std_hr_5_3_0"] = new { x = -17, y = 79 } },
            spritesheet = new
            {
                frames = new Dictionary<string, object>
                {
                    ["hair_h_std_hr_5_3_0"] = new
                    {
                        frame = new { x = 0, y = 0, w = 2, h = 2 },
                        rotated = false,
                        trimmed = true,
                        spriteSourceSize = new { x = 3, y = 1, w = 2, h = 2 },
                        sourceSize = new { w = 5, h = 6 },
                    },
                },
                meta = new { image = "hair.png" },
            },
        });
        var hab = HabFile.Read(HabBuilder.Build("hair", ("hair.json", "application/json", json), ("hair.png", "image/png", sheet)));

        using var library = HabLibraryReader.Read(hab, "fallback");

        var image = library.Images.Single().Pixels!;
        Assert.Equal((5, 6), (image.Width, image.Height));
        Assert.Equal(SKColors.Red, image.GetPixel(3, 1));
        Assert.Equal(SKColors.Red, image.GetPixel(4, 2));
        Assert.Equal(0, image.GetPixel(2, 1).Alpha);
        Assert.Equal(0, image.GetPixel(0, 0).Alpha);
    }

    [Fact]
    public void ASheetLibrarysDocumentNamesTheAssetsDrawnInAPalette()
    {
        var sheet = TestImages.Png(4, 2, (_, _) => SKColors.Green);
        var json = HabBuilder.Json(new
        {
            name = "dog",
            documentClass = "dog",
            assets = new Dictionary<string, object>
            {
                ["dog_64_a_0_0"] = new { x = 15, y = 26, usesPalette = true },
                ["dog_64_sd_0_0"] = new { x = 10, y = 4 },
            },
            spritesheet = new
            {
                frames = new Dictionary<string, object>
                {
                    ["dog_dog_64_a_0_0"] = new { frame = new { x = 0, y = 0, w = 2, h = 2 }, rotated = false },
                    ["dog_dog_64_sd_0_0"] = new { frame = new { x = 2, y = 0, w = 2, h = 2 }, rotated = false },
                },
                meta = new { image = "dog.png" },
            },
        });
        var hab = HabFile.Read(HabBuilder.Build("dog", ("dog.json", "application/json", json), ("dog.png", "image/png", sheet)));

        using var library = HabLibraryReader.Read(hab, "fallback");

        var document = System.Text.Json.Nodes.JsonNode.Parse(Assert.Single(library.Documents).Text)!;
        Assert.Equal(["dog_64_a_0_0"], document["paletteAssets"]!.AsArray().Select(n => n!.GetValue<string>()));
    }

    [Fact]
    public void AFlashLibrarysAssetsDocumentGivesOffsetsAndMirroredAliases()
    {
        var assets = """<assets><asset name="door_64" x="16" y="86"/><asset name="door_64_flipH" source="door_64" flipH="1" x="16" y="86"/></assets>"""u8.ToArray();
        var hab = HabFile.Read(HabBuilder.Build("HabboRoomContent",
            ("room_assets", "text/xml", assets),
            ("door_64", "image/png", TestImages.Png(2, 3, (_, _) => SKColors.Black))));

        using var library = HabLibraryReader.Read(hab, "fallback");

        var door = library.Images.Single(i => i.Name == "door_64");
        Assert.Equal((16, 86, true), (door.OffsetX, door.OffsetY, door.HasOffset));
        var flipped = library.Images.Single(i => i.Name == "door_64_flipH");
        Assert.Equal(("door_64", true, 16), (flipped.Source, flipped.FlipH, flipped.OffsetX));
    }

    [Fact]
    public void AFlashLibraryKeepsItsXmlAndItsImagesWithTheManifestOffsets()
    {
        var manifest = """<manifest><library><assets><asset name="door_32" mimeType="image/png"><param key="offset" value="-8,32"/></asset></assets></library></manifest>"""u8.ToArray();
        var hab = HabFile.Read(HabBuilder.Build("HabboRoomContent",
            ("index", "text/xml", """<object type="room" visualization="room" logic="room"/>"""u8.ToArray()),
            ("manifest", "text/xml", manifest),
            ("door_32", "image/png", TestImages.Png(2, 3, (_, _) => SKColors.White)),
            ("wall_texture_64_1", "image/png", TestImages.Png(4, 4, (_, _) => SKColors.Gray))));

        using var library = HabLibraryReader.Read(hab, "fallback");

        Assert.Equal("HabboRoomContent", library.Name);
        Assert.Equal(["index", "manifest"], library.Documents.Select(d => d.Name).Order());
        var door = library.Images.Single(i => i.Name == "door_32");
        Assert.Equal((-8, 32, true), (door.OffsetX, door.OffsetY, door.HasOffset));
        Assert.False(library.Images.Single(i => i.Name == "wall_texture_64_1").HasOffset);
    }
}
