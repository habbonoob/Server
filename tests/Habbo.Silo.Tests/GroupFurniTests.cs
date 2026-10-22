using Habbo.Silo.Items;
using Habbo.Silo.Items.Interactions;

namespace Habbo.Silo.Tests;

public class GroupFurniTests
{
    [Fact]
    public void AGroupGateLetsOnlyItsGroupsMembersThrough()
    {
        Assert.True(GroupGateInteraction.LetsThrough(7, new HashSet<int> { 3, 7 }));
        Assert.False(GroupGateInteraction.LetsThrough(7, new HashSet<int> { 3 }));
        Assert.False(GroupGateInteraction.LetsThrough(7, null));
        Assert.False(GroupGateInteraction.LetsThrough(null, new HashSet<int> { 7 }));
    }

    [Fact]
    public void NewGroupFurniCarriesItsGroupAndLeavesTheBadgeAndColoursToTheGroup()
    {
        var data = GroupFurniData.Of(GroupFurniData.FirstState, 42);

        Assert.Equal(["0", "42", "", "", ""], data.StringValues!);
    }

    [Fact]
    public void ChangingTheStateKeepsTheRest()
    {
        var data = GroupFurniData.Of("0", 42) with { StringValues = ["0", "42", "b05114s19", "ffffff", "000000"] };

        Assert.Equal(["1", "42", "b05114s19", "ffffff", "000000"], GroupFurniData.WithState(data, "1").StringValues!);
    }
}
