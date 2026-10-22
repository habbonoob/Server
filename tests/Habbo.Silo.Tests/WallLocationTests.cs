using Habbo.Protocol.Rooms;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Tests;

public class WallLocationTests
{
    private static readonly RoomModel Model = RoomModelParser.Parse("xxxxx\rx0000\rx0000\rx0000", doorX: 1, doorY: 1);

    [Theory]
    [InlineData(":w=0,3 l=5,22 l", ":w=0,3 l=5,22 l")]
    [InlineData(" :w=2,0 l=-4,30 r ", ":w=2,0 l=-4,30 r")]
    public void AWellFormedLocationOnTheRoomsWallsIsKeptInTheClientsForm(string sent, string kept)
    {
        Assert.True(WallLocation.TryParse(sent, Model, out var location));
        Assert.Equal(kept, location.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(":w=0,3 l=5,22")]
    [InlineData(":w=0,3 l=5,22 x")]
    [InlineData(":w=99,3 l=5,22 l")]
    [InlineData(":w=0,3 l=5,9999 l")]
    [InlineData(":w=0,3 l=5,22 l\u0000junk")]
    public void AnythingElseIsRefused(string sent) => Assert.False(WallLocation.TryParse(sent, Model, out _));
}
