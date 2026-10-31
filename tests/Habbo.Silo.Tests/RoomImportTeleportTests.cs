using Habbo.Silo.Rooms.Copies;

namespace Habbo.Silo.Tests;

/// <summary>Copied teleports paired as Habbo linked them: each names its partner's id there in its extra number, as
/// measured on habbo.com (a pair bought together: 925862584 and 925862585, each the other's extra).</summary>
public class RoomImportTeleportTests
{
    [Fact]
    public void Two_teleports_naming_each_other_are_a_pair()
    {
        var pairs = CopiedTeleports.Pair([(925862584, 925862585), (925862585, 925862584)]);

        Assert.Equal([(925862584L, 925862585L)], pairs);
    }

    [Fact]
    public void A_teleport_whose_partner_stood_in_another_room_is_in_no_pair()
    {
        Assert.Empty(CopiedTeleports.Pair([(925862584, 925862585), (925862581, 925862582)]));
    }

    [Fact]
    public void Only_teleports_naming_each_other_go_together()
    {
        // 581 names 584, which is 585's: 584 and 585 are the pair
        var pairs = CopiedTeleports.Pair([(925862584, 925862585), (925862585, 925862584), (925862581, 925862584)]);

        Assert.Equal([(925862584L, 925862585L)], pairs);
    }

    [Fact]
    public void A_copy_from_before_extra_numbers_pairs_nothing()
    {
        Assert.Empty(CopiedTeleports.Pair([(925862584, 0), (925862585, 0), (0, 0)]));
    }

    [Fact]
    public void A_teleport_naming_itself_is_in_no_pair()
    {
        Assert.Empty(CopiedTeleports.Pair([(925862584, 925862584)]));
    }
}
