using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Silo.Items.Interactions;

/// <summary>A Freeze ice block (es_box): whole (state 0) it stands in the way; a blast breaks it into a
/// power-up (its state the power-up's type) that the first to step on picks up (type + 10). Its custom params
/// are the odds of each type. The room's games run it (<see cref="Rooms.FreezeArena"/>).</summary>
[FurniInteraction(Name)]
public sealed class FreezeBlockInteraction : FurniInteraction
{
    public const string Name = "freeze_block";

    /// <summary>Whether the block is whole, and so keeps avatars off its tile.</summary>
    public static bool Whole(FurniDataView data) => data.StateText is "" or "0";
}
