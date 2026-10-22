using System.IO.Compression;
using System.Text;
using Habbo.Camera;

namespace Habbo.Gateway.Tests.Camera;

/// <summary>RenderRoom's data as the client writes it (RenderRoomMessageComposer.compressData): its JSON, its keys
/// spelled out by magic strings, zlib-deflated.</summary>
public class RenderRequestDecoderTests
{
    private const string Json =
        """{ "planes" : [{"z":12.5,"cornerPoints":[{"x":320,"y":320},{"x":0,"y":320},{"x":320,"y":0},{"x":0,"y":0}],"color":4278190080,"bottomAligned":false,"texCols":[]},""" +
        """{"z":3,"cornerPoints":[{"x":1,"y":2},{"x":3,"y":4},{"x":5,"y":6},{"x":7,"y":8}],"color":16777215,"masks":[{"name":"window_64","location":{"x":10,"y":20},"flipH":true,"flipV":false}],"bottomAligned":true,"texCols":[{"assetNames":["wall_64",""]}]}]""" +
        ""","sprites" : [{"name":"chair_polyfon_64_a_2_0","x":12,"y":-4,"z":20.25,"flipH":true,"color":16777215,"alpha":128,"blendMode":"add"},""" +
        """{"name":"http://localhost:8080/photos/a.png","x":1,"y":2,"z":3,"skew":-0.5,"frame":true,"width":40,"height":30}]""" +
        ""","modifiers" : {},"filters" : [{"name":"dark_sepia","alpha":128},{"name":"frame_gold"}],"roomid" : 27,"zoom" : 2,"status" : 5,"timestamp" : 1700000000123,"checksum" : 4242 }""";

    private static byte[] Deflate(string text)
    {
        var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
            zlib.Write(Encoding.UTF8.GetBytes(text));
        return output.ToArray();
    }

    [Fact]
    public void TheClientsPhotoIsReadWhole()
    {
        var request = RenderRequestDecoder.Decode(Deflate(Json));

        Assert.Equal(27, request.RoomId);
        Assert.Equal(2, request.Zoom);
        Assert.Equal(2, request.Planes.Count);
        Assert.Equal(4278190080u, request.Planes[0].Color);

        var wall = request.Planes[1];
        Assert.True(wall.BottomAligned);
        Assert.Equal(["wall_64", ""], wall.TexCols![0].AssetNames);
        var mask = Assert.Single(wall.Masks!);
        Assert.Equal(("window_64", 10, 20, true, false), (mask.Name, mask.Location.X, mask.Location.Y, mask.FlipH, mask.FlipV));

        var chair = request.Sprites[0];
        Assert.Equal(("chair_polyfon_64_a_2_0", 12.0, -4.0, 20.25, true, 128, "add"), (chair.Name, chair.X, chair.Y, chair.Z, chair.FlipH, chair.Alpha, chair.BlendMode));
        var picture = request.Sprites[1];
        Assert.Equal((-0.5, true, 40, 30), (picture.Skew, picture.Frame, picture.Width, picture.Height));

        Assert.Equal(["dark_sepia", "frame_gold"], request.Filters!.Select(f => f.Name));
        Assert.Null(request.Filters![1].Alpha);
    }

    [Fact]
    public void WithNoZoomThePhotoIsAsSeen()
    {
        var request = RenderRequestDecoder.Decode(Deflate(Json.Replace(""","zoom" : 2""", "")));
        Assert.Equal(1, request.Zoom);
    }

    [Fact]
    public void WhatIsNotDeflatedIsRefused() =>
        Assert.ThrowsAny<InvalidDataException>(() => RenderRequestDecoder.Decode("not zlib"u8));
}
