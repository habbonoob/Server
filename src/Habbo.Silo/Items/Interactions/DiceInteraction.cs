using System.Globalization;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Habbo.Protocol.Messages.Outgoing.Room.Furniture;
using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Silo.Items.Interactions;

/// <summary>A dice: thrown by anyone standing next to it, rolls for a moment, then lands on a
/// face. The faces are the item's states from 1 upwards — 1 to 6 for an ordinary dice.</summary>
[FurniInteraction("furniture_dice")]
public sealed class DiceInteraction : FurniInteraction
{
    private const int RollingState = -1;
    private const int OffState = 0;
    private const int FirstFace = 1;

    /// <summary>How long a throw rolls before it lands. The client animates the roll for as long
    /// as the dice stays in the rolling state, so this is the whole of the suspense.</summary>
    private static readonly TimeSpan RollDuration = TimeSpan.FromMilliseconds(1500);

    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Everyone;

    public override Task UseAsync(FurniUse use)
    {
        if (!use.Room.IsWithinReach(use.User, use.Item) || use.Item.IsBusy)
            return Task.CompletedTask;

        return use.Action switch
        {
            FurniAction.ThrowDice => ThrowAsync(use),
            FurniAction.DiceOff => ShowAsync(use, OffState),
            _ => Task.CompletedTask,
        };
    }

    private static async Task ThrowAsync(FurniUse use)
    {
        var faces = Faces(use);
        if (faces.Count == 0)
            return;

        use.Item.BusyUntil = DateTime.UtcNow + RollDuration;
        // rolling is only shown: picked up mid-roll, the dice keeps the face it had rather than rolling for ever after
        await ShowAsync(use, RollingState, persist: false);
        use.Room.Schedule(RollDuration, async () =>
        {
            if (use.Room.IsPlaced(use.Item))
                await ShowAsync(use, faces[Random.Shared.Next(faces.Count)]);
        });
    }

    private static async Task ShowAsync(FurniUse use, int value, bool persist = true)
    {
        // the item's own data with the new face, so a limited edition keeps its number
        var data = use.Item.Data with { State = value.ToString(CultureInfo.InvariantCulture) };
        if (persist)
            await use.Room.SaveAsync(use.Item, data);
        else
            use.Item.Data = data;
        use.Room.Broadcast(new DiceValueMessageComposer(use.Item.Item.Id, value));
    }

    /// <summary>The unbroken run of states from 1: a dice's SWF draws its faces there, and keeps
    /// anything it draws beyond them — a bottle's other looks — out of that run.</summary>
    private static List<int> Faces(FurniUse use)
    {
        var states = use.Item.Item.Definition!.States.Select(s => s.State).ToHashSet();
        var faces = new List<int>();
        for (var face = FirstFace; states.Contains(face); face++)
            faces.Add(face);
        return faces;
    }
}
