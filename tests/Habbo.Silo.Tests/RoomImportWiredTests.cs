using Habbo.Domain.Furniture.Data;
using Habbo.Domain.Rooms.Copies;
using Habbo.Silo.Rooms.Copies;
using Habbo.Silo.Wired;
using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Tests;

/// <summary>A copied room's wired as the import sets it up: Habbo's variable ids found again by name, and the
/// settings checked as a save checks them.</summary>
public class RoomImportWiredTests
{
    private static readonly WiredBoxRegistry Boxes = new();

    private static RoomCopyVariable Made(string id, string name, int target = WiredVariableTargets.User) =>
        new(id, WiredVariableKinds.CreatedByUser, name, target);

    [Fact]
    public void A_room_variable_becomes_its_box_by_name_not_by_number()
    {
        // Habbo numbers variables apart from furni: 52968 is no furni's id
        var map = CopiedVariables.Map([Made("52968", "health")], new Dictionary<string, int> { ["health"] = 7 });

        Assert.Equal("7", map["52968"]);
    }

    [Fact]
    public void Names_are_matched_whatever_their_case()
    {
        var map = CopiedVariables.Map([Made("52968", "Health")], new Dictionary<string, int> { ["health"] = 7 });

        Assert.Equal("7", map["52968"]);
    }

    [Fact]
    public void A_built_in_variable_becomes_this_hotels_own()
    {
        var map = CopiedVariables.Map(
            [new RoomCopyVariable("-230", WiredVariableKinds.Internal, "@id", WiredVariableTargets.Furni)],
            new Dictionary<string, int>());

        Assert.Equal(WiredInternalVariables.IdOf(WiredVariableTargets.Furni, "@id"), map["-230"]);
    }

    [Fact]
    public void A_built_in_variable_this_hotel_lacks_is_left_out()
    {
        var map = CopiedVariables.Map(
            [new RoomCopyVariable("-999", WiredVariableKinds.Internal, "@no_such_thing", WiredVariableTargets.Furni)],
            new Dictionary<string, int>());

        Assert.Empty(map);
    }

    [Fact]
    public void A_part_of_a_variable_follows_its_parent()
    {
        var map = CopiedVariables.Map(
            [Made("52968", "xp"), new RoomCopyVariable("52968.level", WiredVariableKinds.SubVariable, "xp.level", WiredVariableTargets.User)],
            new Dictionary<string, int> { ["xp"] = 7 });

        Assert.Equal("7.level", map["52968.level"]);
    }

    [Fact]
    public void A_variable_whose_box_was_not_copied_is_left_out()
    {
        var map = CopiedVariables.Map([Made("52968", "health")], new Dictionary<string, int>());

        Assert.Empty(map);
    }

    [Fact]
    public void Settling_holds_an_effects_delay_to_what_the_dialog_offers()
    {
        var settings = new FurniWired { DelayInPulses = int.MaxValue };

        Assert.Null(RoomWired.Settle(Boxes.For("wf_act_toggle_state")!, settings));
        Assert.Equal(20, settings.DelayInPulses);
    }

    [Fact]
    public void Settling_gives_a_slot_its_default_for_a_source_it_does_not_offer()
    {
        var type = Boxes.For("wf_act_toggle_state")!;
        var settings = new FurniWired { FurniSources = [999] };

        RoomWired.Settle(type, settings);

        Assert.Equal(type.FurniSlots.Select(slot => slot.Default), settings.FurniSources);
    }

    [Fact]
    public void Settling_runs_the_boxs_own_checks()
    {
        var settings = new FurniWired { IntParams = [0, 0, 0, 1], StringParam = "0,ACH_Login1,100" };

        Assert.NotNull(RoomWired.Settle(Boxes.For("wf_act_give_reward")!, settings));
    }
}
