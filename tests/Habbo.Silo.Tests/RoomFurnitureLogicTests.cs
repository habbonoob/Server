using Habbo.Domain.Users;
using Habbo.Protocol.Rooms;
using Habbo.Silo.Items;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;
using Habbo.Silo.Users;

namespace Habbo.Silo.Tests;

/// <summary>The rules behind room furniture that need no room: outfits, the dimmer's state string,
/// the changing gate's two outfits, and the floor plan editor's checks.</summary>
public class RoomFurnitureLogicTests
{
    private static readonly string[] Clothes = ["ch", "lg", "sh"];

    [Fact]
    public void AnOutfitKeepsOnlyTheClothes()
    {
        Assert.Equal("ch-215-62.lg-285-91", FigureOutfit.Clothing("hr-115-42.hd-190-1.ch-215-62.lg-285-91", Clothes));
    }

    [Fact]
    public void WearingAnOutfitReplacesTheSameSetTypesAndKeepsTheRest()
    {
        Assert.Equal("hr-115-42.hd-190-1.sh-290-62.ch-3030-82.lg-275-82",
            FigureOutfit.Wear("hr-115-42.hd-190-1.ch-215-62.lg-285-91.sh-290-62", "ch-3030-82.lg-275-82"));
    }

    [Fact]
    public void TheDimmerStateReadsBackWhatItWrites()
    {
        var state = new DimmerState(true, 2, 1, "#EA4532", 200);
        Assert.Equal("2,2,1,#EA4532,200", state.ToString());
        Assert.Equal(state, DimmerState.Parse(state.ToString()));
        Assert.Null(DimmerState.Parse("broken"));
    }

    [Fact]
    public void AChangingGateKeepsAnOutfitPerGender()
    {
        var state = ClothingChangeInteraction.WithOutfit(string.Empty, Gender.Female, "ch-1");
        state = ClothingChangeInteraction.WithOutfit(state, Gender.Male, "ch-2");

        Assert.Equal("ch-2,ch-1", state);
        Assert.Equal("ch-1", ClothingChangeInteraction.Outfit(state, Gender.Female));
        Assert.Equal("ch-2", ClothingChangeInteraction.Outfit(state, Gender.Male));
    }

    private static readonly RoomOptions Limits = new() { FloorPlanMaxAxis = 64, FloorPlanMaxArea = 3025, FloorPlanMaxWallHeight = 16 };

    private static readonly RoomModel Current = RoomModelParser.Parse(Plan, doorX: 1, doorY: 0);

    /// <summary>A room whose first row and column are wall, but for the door.</summary>
    private const string Plan = "x0x\rx00\rx00";

    private static List<string> Check(FloorPlan plan, IEnumerable<(int X, int Y)>? kept = null) =>
        FloorPlanValidator.Validate(plan, Limits, largePlansAllowed: false, kept ?? [], Current, out _);

    [Fact]
    public void AGoodPlanPasses()
    {
        Assert.Empty(Check(new FloorPlan(Plan, 1, 0, 2, 0, 0, -1)));
    }

    [Fact]
    public void AnEntryOffTheFloorAndABadDirectionAreBothReported()
    {
        var errors = Check(new FloorPlan(Plan, 0, 0, 9, 0, 0, -1));

        Assert.Contains(FloorPlanValidator.EntryNotOnTile, errors);
        Assert.Contains(FloorPlanValidator.InvalidEntryTileDirection, errors);
    }

    [Fact]
    public void TheFirstRowHoldsOnlyTheDoor()
    {
        Assert.Contains(FloorPlanValidator.InvalidDoorSetup, Check(new FloorPlan("00x\rx00\rx00", 1, 0, 2, 0, 0, -1)));
    }

    [Fact]
    public void ATileUnderFurnitureMustStayAsItIs()
    {
        Assert.Contains(FloorPlanValidator.ChangeBlockedByRoomItem,
            Check(new FloorPlan("x0x\rx10\rx00", 1, 0, 2, 0, 0, -1), kept: [(1, 1)]));
    }
}
