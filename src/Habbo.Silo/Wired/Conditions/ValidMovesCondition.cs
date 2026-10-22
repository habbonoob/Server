namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when every move the stack's movement effects are about to make can be made: no
/// furni would go off the room or into something it may not stand on. The effects then make
/// exactly the moves checked.</summary>
[WiredBox("wf_cnd_valid_moves")]
public sealed class ValidMovesCondition : WiredCondition
{
    public override int Code => ConditionCodes.CanPerformMove;

    public override bool Passes(WiredBox box, WiredRun run) =>
        run.Stack
            .Where(other => other.Type is WiredMoveEffect { IsNegative: false })
            .All(other => ((WiredMoveEffect)other.Type).PlanFor(other, run)
                .All(move => run.Room.CanMoveItem(move.Item, move.X, move.Y, move.Rotation,
                    run.MoveOptions(((WiredMoveEffect)other.Type).AvatarsBlock))));
}
