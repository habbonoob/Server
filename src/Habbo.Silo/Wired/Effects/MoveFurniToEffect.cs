namespace Habbo.Silo.Wired.Effects;

/// <summary>Moves the moving furni (its first furni slot) beside a target furni (its second, one at
/// random): the chosen number of tiles (1 to 5) from it in the chosen direction.</summary>
[WiredBox("wf_act_move_furni_to")]
public sealed class MoveFurniToEffect : WiredMoveEffect
{
    private const int Direction = 0;
    private const int Tiles = 1;
    private const int MaxTiles = 5;

    public override int Code => EffectCodes.MoveFurniTo;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.EitherPicks, WiredSlot.EitherPicks.DefaultingTo(WiredSources.PickedFurni2)];
    public override IReadOnlyList<int> DefaultIntParams => [0, 1];

    protected override IReadOnlyList<WiredMove> Plan(WiredBox box, WiredRun run)
    {
        var targets = run.Furni(box, 1).ToList();
        if (targets.Count == 0)
            return [];

        var moves = new List<WiredMove>();
        foreach (var item in run.Furni(box))
        {
            var target = targets[run.Room.Random.Next(targets.Count)];
            var (x, y) = (target.X, target.Y);
            for (var step = 0; step < Math.Clamp(box.IntParam(Tiles, 1), 1, MaxTiles); step++)
                (x, y) = WiredFurni.Step(x, y, box.IntParam(Direction));
            moves.Add(new WiredMove(item, x, y, item.Rotation));
        }

        return moves;
    }
}
