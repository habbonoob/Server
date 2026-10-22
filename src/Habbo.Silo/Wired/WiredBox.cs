using Habbo.Domain.Furniture.Data;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired;

/// <summary>A wired box placed in a room: the item, what kind of box it is, the settings its
/// dialog saved, and what it keeps between pulses. Settings are replaced whole on every save.</summary>
public sealed class WiredBox(RoomItemStore.PlacedItem item, WiredBoxType type, FurniWired settings)
{
    public RoomItemStore.PlacedItem Item { get; } = item;
    public WiredBoxType Type { get; } = type;
    public FurniWired Settings { get; set; } = settings;

    public int Id => Item.Item.Id;

    /// <summary>A periodic trigger's countdown: pulses until it fires next.</summary>
    public int PulsesUntilNext { get; set; }

    /// <summary>For an execution limit: the pulses its stack ran on, within its window.</summary>
    public List<int> RecentRuns { get; } = [];

    /// <summary>For a random add-on: the effects it picked, by run, most recent last.</summary>
    public List<List<int>> RecentPicks { get; } = [];

    /// <summary>For the unseen-effect add-on: how many effects of its stack have run in turn.</summary>
    public int Rotation { get; set; }

    /// <summary>For move-to-direction: the direction each of its furni is heading.</summary>
    public Dictionary<int, int> FurniDirections { get; } = [];

    /// <summary>For boxes limited per game: the game counted, and how many times it ran in it, for each it counts.</summary>
    public int CountedGame { get; set; }
    public Dictionary<int, int> TimesThisGame { get; } = [];

    /// <summary>Forgets what the box kept between pulses, for settings saved anew: a direction, picks or counts made
    /// under the old ones would carry on as if nothing had changed.</summary>
    public void ResetRunState()
    {
        PulsesUntilNext = 0;
        RecentRuns.Clear();
        RecentPicks.Clear();
        Rotation = 0;
        FurniDirections.Clear();
        CountedGame = 0;
        TimesThisGame.Clear();
    }

    /// <summary>Where the box takes a furni slot's furni from: as saved, or the slot's default.</summary>
    public int FurniSource(int slot = 0) =>
        Settings.FurniSources.Count == Type.FurniSlots.Count && slot < Settings.FurniSources.Count ? Settings.FurniSources[slot]
        : slot < Type.FurniSlots.Count ? Type.FurniSlots[slot].Default : WiredSources.PickedFurni;

    /// <summary>Where the box takes a user slot's users from: as saved, or the slot's default.</summary>
    public int UserSource(int slot = 0) =>
        Settings.UserSources.Count == Type.UserSlots.Count && slot < Settings.UserSources.Count ? Settings.UserSources[slot]
        : slot < Type.UserSlots.Count ? Type.UserSlots[slot].Default : WiredSources.Triggerer;

    /// <summary>An int parameter, or the fallback when the dialog did not send that many.</summary>
    public int IntParam(int index, int fallback = 0) => IntParams is var ints && index < ints.Count ? ints[index] : fallback;

    /// <summary>The box's int parameters: as saved, or a new box's, as Habbo gives every box it places.</summary>
    public IReadOnlyList<int> IntParams => Settings.IntParams.Count > 0 ? Settings.IntParams : Type.DefaultIntParams;

    /// <summary>Whether a checkbox-style int parameter is ticked.</summary>
    public bool Flag(int index) => IntParam(index) != 0;
}
