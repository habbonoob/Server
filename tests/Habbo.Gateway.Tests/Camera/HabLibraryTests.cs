using Habbo.Camera;
using SkiaSharp;

namespace Habbo.Gateway.Tests.Camera;

/// <summary>A library's assets as the client cuts them from its .hab: an alias draws its source's frame, which sits
/// at its trim offset in a canvas of its untrimmed size.</summary>
public class HabLibraryTests
{
    // a 3x2 sheet: red, green, blue on top; white below
    private static readonly byte[] Sheet = TestImages.Png(3, 2, (x, y) => y == 1 ? SKColors.White : x switch
    {
        0 => SKColors.Red,
        1 => SKColors.Lime,
        _ => SKColors.Blue,
    });

    private static HabLibrary Chair() => HabLibrary.Read(HabBuilder.Build("chair",
        ("chair.json", "application/json", HabBuilder.Json(new
        {
            name = "chair",
            assets = new Dictionary<string, object>
            {
                ["chair_64_a_2_0"] = new { x = 5, y = 7 },
                ["chair_64_a_4_0"] = new { source = "chair_64_a_2_0", x = 5, y = 7, flipH = true },
            },
            spritesheet = new
            {
                meta = new { image = "chair.png" },
                frames = new Dictionary<string, object>
                {
                    // the frame is the sheet's green and blue column pair, trimmed of a pixel all round
                    ["chair_chair_64_a_2_0"] = new
                    {
                        frame = new { x = 1, y = 0, w = 2, h = 2 },
                        spriteSourceSize = new { x = 1, y = 1, w = 2, h = 2 },
                        sourceSize = new { w = 4, h = 4 },
                    },
                },
            },
        })),
        ("chair.png", "image/png", Sheet)));

    [Fact]
    public void AnAssetIsItsFrameInItsUntrimmedSize()
    {
        using var chair = Chair();
        var asset = chair.Get("chair_64_a_2_0")!;

        Assert.Equal((4, 4), (asset.Bitmap.Width, asset.Bitmap.Height));
        Assert.Equal(SKColors.Transparent.Alpha, asset.Bitmap.GetPixel(0, 0).Alpha);
        Assert.Equal(SKColors.Lime, asset.Bitmap.GetPixel(1, 1));
        Assert.Equal(SKColors.Blue, asset.Bitmap.GetPixel(2, 1));
        Assert.Equal(SKColors.White, asset.Bitmap.GetPixel(1, 2));
        Assert.Equal((5, 7, false), (asset.OffsetX, asset.OffsetY, asset.FlipH));
    }

    [Fact]
    public void AnAliasDrawsItsSourcesPixelsAndSaysItIsFlipped()
    {
        using var chair = Chair();
        var alias = chair.Get("chair_64_a_4_0")!;

        Assert.True(alias.FlipH);
        Assert.Same(chair.Get("chair_64_a_2_0")!.Bitmap, alias.Bitmap);
    }

    [Fact]
    public void AnAssetTheLibraryLacksIsNone()
    {
        using var chair = Chair();
        Assert.False(chair.Has("chair_64_b_2_0"));
        Assert.Null(chair.Get("chair_64_b_2_0"));
    }

    [Fact]
    public void AConvertedLibrarysImagesAreAssetsByTheirNamesAndItsXmlNamesItsAliases()
    {
        var assetsXml = """
            <?xml version="1.0" encoding="ISO-8859-1" ?>
            <assets>
               <asset name="landscape_1"/>
               <asset name="landscape_1_flipH" source="landscape_1" flipH="1"/>
            </assets>
            """u8.ToArray();
        using var room = HabLibrary.Read(HabBuilder.Build("room",
            ("room_assets", "text/xml", assetsXml),
            ("landscape_1", "image/png", TestImages.Png(2, 1, (x, _) => x == 0 ? SKColors.Red : SKColors.Blue))));

        Assert.Equal(SKColors.Red, room.Get("landscape_1")!.Bitmap.GetPixel(0, 0));
        var flipped = room.Get("landscape_1_flipH")!;
        Assert.True(flipped.FlipH);
        Assert.Equal(SKColors.Red, flipped.Bitmap.GetPixel(0, 0));
    }
}
