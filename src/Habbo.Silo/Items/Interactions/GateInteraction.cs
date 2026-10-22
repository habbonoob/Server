using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Silo.Items.Interactions;

/// <summary>A gate: those with rights open and close it, and avatars walk through it only while it is open. It does
/// not close on anyone standing in it.</summary>
[FurniInteraction(Name)]
public sealed class GateInteraction : FurniInteraction
{
    public const string Name = "gate";

    public const string Closed = "0";
    public const string Open = "1";

    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Controllers;

    public override Task UseAsync(FurniUse use)
    {
        if (use.Action != FurniAction.Use)
            return Task.CompletedTask;

        var open = use.Item.Data.State == Open;
        if (open && use.Room.IsOccupied(use.Item))
            return Task.CompletedTask;

        return use.Room.UpdateAsync(use.Item, use.Item.Data with { State = open ? Closed : Open });
    }
}
