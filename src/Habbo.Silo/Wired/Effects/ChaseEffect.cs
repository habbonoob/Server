namespace Habbo.Silo.Wired.Effects;

/// <summary>Moves each of its furni one tile towards the nearest user in its row or column ("towards nearest user in
/// the same row or column", Habbo's own text); running into them is a collision. With no one in line it takes a step
/// in a random straight direction: Habbo moved one away from the only user, who stood diagonally (experiment.chase,
/// one run, so a hypothesis).</summary>
[WiredBox("wf_act_chase")]
public class ChaseEffect : WiredEffect
{
    private static readonly (int X, int Y)[] Straight = [(0, -1), (1, 0), (0, 1), (-1, 0)];

    public override int Code => EffectCodes.Chase;
    public override bool PicksFurni => true;

    /// <summary>+1 towards the user, -1 away.</summary>
    protected virtual int Towards => 1;

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        foreach (var item in run.Furni(box).ToList())
        {
            // in its row or its column, not on its tile
            var nearest = run.Room.Entities.Entities
                .Where(user => (user.X == item.X) != (user.Y == item.Y))
                .MinBy(user => Math.Abs(user.X - item.X) + Math.Abs(user.Y - item.Y));

            if (nearest is null)
            {
                foreach (var (sx, sy) in Straight.OrderBy(_ => run.Room.Random.Next()))
                {
                    if (await run.MoveAsync(item, item.X + sx, item.Y + sy, item.Rotation))
                        break;
                }

                continue;
            }

            var (x, y) = (item.X + Math.Sign(nearest.X - item.X) * Towards, item.Y + Math.Sign(nearest.Y - item.Y) * Towards);
            if (Towards > 0 && (x, y) == (nearest.X, nearest.Y))
                await run.Room.CollideAsync(nearest, item);
            else
                await run.MoveAsync(item, x, y, item.Rotation);
        }
    }
}
