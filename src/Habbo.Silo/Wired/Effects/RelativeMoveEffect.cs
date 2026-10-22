namespace Habbo.Silo.Wired.Effects;

/// <summary>Moves each of its furni so many tiles along each axis (negative for the other way).</summary>
[WiredBox("wf_act_rel_mov")]
public sealed class RelativeMoveEffect : WiredMoveEffect
{
    public override int Code => EffectCodes.RelativeFurniMove;
    public override bool AllowsWallFurni => true;
    public override bool PicksFurni => true;
    public override IReadOnlyList<int> DefaultIntParams => [0, 0];

    protected override IReadOnlyList<WiredMove> Plan(WiredBox box, WiredRun run) =>
        run.Furni(box).Select(item => new WiredMove(item, item.X + box.IntParam(0), item.Y + box.IntParam(1), item.Rotation)).ToList();
}
