using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired;

/// <summary>A furni move an effect intends: the item, where to, and turned how.</summary>
public sealed record WiredMove(RoomItemStore.PlacedItem Item, int X, int Y, int Rotation);

/// <summary>An effect that moves furni by a plan it can make before moving anything, so a
/// "valid moves" condition can check the plan and the effect then carry out the same one.</summary>
public abstract class WiredMoveEffect : WiredEffect
{
    /// <summary>Whether an avatar standing where a furni would go blocks it.</summary>
    public virtual bool AvatarsBlock => true;

    /// <summary>Where each furni would go, choosing now whatever the effect chooses at random.</summary>
    protected abstract IReadOnlyList<WiredMove> Plan(WiredBox box, WiredRun run);

    /// <summary>The box's plan for this run: made once, so what a condition checked is what is done.
    /// A delayed box plans when it runs, since the room will have moved on by then.</summary>
    public IReadOnlyList<WiredMove> PlanFor(WiredBox box, WiredRun run)
    {
        if (box.Settings.DelayInPulses > 0)
            return Plan(box, run);

        if (!run.PlannedMoves.TryGetValue(box.Id, out var plan))
            run.PlannedMoves[box.Id] = plan = Plan(box, run);
        return plan;
    }

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        foreach (var move in PlanFor(box, run))
            await run.MoveAsync(move.Item, move.X, move.Y, move.Rotation, AvatarsBlock);
        run.PlannedMoves.Remove(box.Id);
    }
}
