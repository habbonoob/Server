namespace Habbo.Silo.Wired.Effects;

/// <summary>Moves the moving furni onto a target furni (one at random), on top of what is there.</summary>
[WiredBox("wf_act_furni_to_furni")]
public sealed class MoveFurniToFurniEffect : WiredMoveEffect
{
    public override int Code => EffectCodes.MoveFurniToFurni;
    public override bool AllowsWallFurni => true;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.EitherPicks.DefaultingTo(WiredSources.TriggerFurni), WiredSlot.EitherPicks];

    protected override IReadOnlyList<WiredMove> Plan(WiredBox box, WiredRun run)
    {
        var targets = run.Furni(box, 1).ToList();
        if (targets.Count == 0)
            return [];

        var moves = new List<WiredMove>();
        foreach (var item in run.Furni(box))
        {
            var target = targets[run.Room.Random.Next(targets.Count)];
            if (target != item)
                moves.Add(new WiredMove(item, target.X, target.Y, item.Rotation));
        }

        return moves;
    }
}
