using Habbo.Domain.Furniture;

namespace Habbo.Silo.Wired.Effects;

/// <summary>Teleports each of its users onto one of its furni, as Habbo does it: the user sparkles
/// at once, moves when the sparkle has run (two seconds; one with "fast teleportation"), and sparkles
/// on the new tile a little longer before the effect they wore comes back. Onto a furni no one may stand on (an antenna,
/// a table), they land next to it.</summary>
[WiredBox("wf_act_teleport_to")]
public sealed class TeleportToFurniEffect : WiredEffect
{
    private const int FastTeleport = 0;

    // the sparkles, by their names in Habbo's effect map
    private const string Sparkle = "Twinkle";
    private const string FastSparkle = "BlueTwinkle";

    // in pulses of half a second, as Habbo times them
    private const int PulsesBeforeMove = 4;
    private const int PulsesAfterMove = 3;
    private const int FastPulsesBeforeMove = 2;
    private const int FastPulsesAfterMove = 1;

    /// <summary>Where a user lands beside a furni they cannot stand on. Habbo landed them on (x, y - 1) both times it
    /// was measured (destination_geometry); the order of the other three is not known.</summary>
    private static readonly (int X, int Y)[] Beside = [(0, -1), (1, 0), (0, 1), (-1, 0)];

    public override int Code => EffectCodes.Teleport;
    public override bool PicksFurni => true;
    public override bool TargetsUsers => true;
    public override IReadOnlyList<int> DefaultIntParams => [0];

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        var targets = run.Furni(box).Where(item => item.Item.Definition!.Type == ItemType.Floor).ToList();
        if (targets.Count == 0)
            return;

        var fast = box.Flag(FastTeleport);
        var sparkle = await run.Room.FindEffectAsync(fast ? FastSparkle : Sparkle);
        foreach (var user in run.Users(box).ToList())
        {
            var target = targets[run.Room.Random.Next(targets.Count)];
            var worn = user.EffectId;
            if (sparkle is { } effect)
                await run.Room.SetEffectAsync(user, effect);

            run.Room.AfterPulses(fast ? FastPulsesBeforeMove : PulsesBeforeMove, () =>
            {
                if (!run.Room.IsPresent(user) || run.Room.FindItem(target.Item.Id) != target)
                    return Task.CompletedTask;

                if (Landing(target, run.Room.Items) is not var (x, y))
                    return Task.CompletedTask;

                run.Room.WarpUser(user, x, y);
                run.Room.AfterPulses(fast ? FastPulsesAfterMove : PulsesAfterMove, async () =>
                {
                    // unless something dressed them in another effect meanwhile
                    if (sparkle is { } effect && user.EffectId == effect)
                        await run.Room.SetEffectAsync(user, worn);
                });
                return Task.CompletedTask;
            });
        }
    }

    /// <summary>The furni's own tile when one may stand there; otherwise the first free tile beside it, or none.</summary>
    internal static (int X, int Y)? Landing(Rooms.RoomItemStore.PlacedItem target, Rooms.RoomItemStore items)
    {
        if (items.CanStandOn(target.X, target.Y))
            return (target.X, target.Y);

        foreach (var (dx, dy) in Beside)
        {
            if (items.CanStandOn(target.X + dx, target.Y + dy))
                return (target.X + dx, target.Y + dy);
        }

        return null;
    }
}
