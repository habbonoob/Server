using Habbo.Protocol.Rooms;

namespace Habbo.Protocol.Tests;

public class RoomModelParserTests
{
    private const string ModelA =
        "xxxxxxxxxxxx" + "\r" +
        "xxx000000000" + "\r" +   // door at (3,1)
        "xxxx00000000" + "\r" +
        "xxxx00000000" + "\r" +
        "xxxx00000000" + "\r" +
        "xxxx00000000" + "\r" +
        "xxxx00000000" + "\r" +
        "xxxx00000000" + "\r" +
        "xxxx00000000" + "\r" +
        "xxxx00000000" + "\r" +
        "xxxx00000000" + "\r" +
        "xxxx00000000" + "\r" +
        "xxxx00000000" + "\r" +
        "xxxx00000000" + "\r" +
        "xxxxxxxxxxxx" + "\r" +
        "xxxxxxxxxxxx";

    [Fact]
    public void ParsesModelA()
    {
        var model = RoomModelParser.Parse(ModelA, doorX: 3, doorY: 1);

        Assert.Equal(12, model.SizeX);
        Assert.Equal(16, model.SizeY);
        Assert.False(model.IsTile(0, 0));           // void corner
        Assert.True(model.IsTile(4, 1));           // regular floor
        Assert.Equal(0, model.GetHeight(4, 1));
        Assert.True(model.IsTile(3, 1));           // door forced walkable
    }

    [Fact]
    public void DoorOverridesVoidTile()
    {
        var map = "xx" + "\r" + "xx";
        var model = RoomModelParser.Parse(map, doorX: 0, doorY: 0);

        Assert.True(model.IsTile(0, 0));
        Assert.Equal(0, model.DoorHeight);
    }

    [Fact]
    public void EmitsFloorMapRoundTrip()
    {
        var model = RoomModelParser.Parse(ModelA, doorX: 3, doorY: 1);
        Assert.Equal(ModelA, RoomModelParser.EmitFloorMap(model));
    }

    [Fact]
    public void DecodesAllCharClasses()
    {
        // '0'=0, '9'=9, 'a'=10, 'z'=35, 'A'=36, 'Z'=61
        var map = "09azAZ" + "\r" + "09azAZ";
        var model = RoomModelParser.Parse(map, doorX: 0, doorY: 0);

        Assert.Equal(0, model.GetHeight(0, 0));
        Assert.Equal(9, model.GetHeight(1, 0));
        Assert.Equal(10, model.GetHeight(2, 0));
        Assert.Equal(35, model.GetHeight(3, 0));
        Assert.Equal(36, model.GetHeight(4, 0));
        Assert.Equal(61, model.GetHeight(5, 0));
    }

    [Fact]
    public void AcceptsLfLineEndings()
    {
        var model = RoomModelParser.Parse("000\n0x0\n000", doorX: 1, doorY: 1);
        Assert.Equal(3, model.SizeX);
        Assert.Equal(3, model.SizeY);
    }

    [Fact]
    public void WalkableStepRules()
    {
        // Two tiles side by side at heights 0 and 2: not a walkable step.
        var steep = RoomModelParser.Parse("02", doorX: 0, doorY: 0);
        Assert.False(steep.IsWalkableStep(0, 0, 1, 0));

        // Height difference of 1: walkable.
        var gentle = RoomModelParser.Parse("01", doorX: 0, doorY: 0);
        Assert.True(gentle.IsWalkableStep(0, 0, 1, 0));

        // Diagonal with one blocked orthogonal neighbor: not walkable (no corner cutting).
        var corner = RoomModelParser.Parse("00" + "\r" + "x0", doorX: 0, doorY: 0);
        Assert.False(corner.IsWalkableStep(0, 0, 1, 1));
    }

    [Fact]
    public void RejectsDoorOutsideModel()
    {
        Assert.Throws<ArgumentException>(() => RoomModelParser.Parse("00\r00", doorX: 5, doorY: 0));
    }
}
