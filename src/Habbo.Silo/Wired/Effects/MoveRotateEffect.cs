namespace Habbo.Silo.Wired.Effects;

/// <summary>Moves each of its furni one tile and turns it, as the dialog chooses. A furni that
/// cannot go where it would (off the room, into something) stays where it is.</summary>
[WiredBox("wf_act_move_rotate")]
public sealed class MoveRotateEffect : WiredMoveEffect
{
    private const int Movement = 0;
    private const int Rotation = 1;

    public override int Code => EffectCodes.MoveFurni;
    public override bool AllowsWallFurni => true;
    public override bool PicksFurni => true;
    public override IReadOnlyList<int> DefaultIntParams => [0, 0, 1];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    protected override IReadOnlyList<WiredMove> Plan(WiredBox box, WiredRun run)
    {
        var random = run.Room.Random;
        var moves = new List<WiredMove>();
        foreach (var item in run.Furni(box))
        {
            var (x, y) = WiredFurni.MoveDirection(box.IntParam(Movement), random) is { } direction
                ? WiredFurni.Step(item.X, item.Y, direction)
                : (item.X, item.Y);
            var rotation = WiredFurni.Rotate(item.Rotation, box.IntParam(Rotation), random);

            if (x != item.X || y != item.Y || rotation != item.Rotation)
                moves.Add(new WiredMove(item, x, y, rotation));
        }

        return moves;
    }
}
