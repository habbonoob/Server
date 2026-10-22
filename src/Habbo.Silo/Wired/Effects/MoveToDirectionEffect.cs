namespace Habbo.Silo.Wired.Effects;

/// <summary>Moves each of its furni one tile in its current direction, which starts as the dialog's
/// and changes, when the way is blocked, as the dialog says: wait, turn 45 or 90 degrees right or
/// left, turn back, or turn to a random direction. A user in the way is a collision; the furni
/// stops there if the dialog says so, and otherwise treats the user as a block.</summary>
[WiredBox("wf_act_move_to_dir")]
public sealed class MoveToDirectionEffect : WiredEffect
{
    private const int StartDirection = 0;
    private const int WhenBlocked = 1;
    private const int StopOnCollision = 2;

    // turn.0 to turn.6
    private const int Wait = 0;
    private const int Right45 = 1;
    private const int Right90 = 2;
    private const int Left45 = 3;
    private const int Left90 = 4;
    private const int Back = 5;

    public override int Code => EffectCodes.MoveToDirection;
    public override bool PicksFurni => true;
    public override IReadOnlyList<int> DefaultIntParams => [0, Wait, 1];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        foreach (var item in run.Furni(box).ToList())
        {
            var direction = box.FurniDirections.GetValueOrDefault(item.Item.Id, box.IntParam(StartDirection) % 8);
            var (x, y) = WiredFurni.Step(item.X, item.Y, direction);

            if (run.Room.Entities.Entities.FirstOrDefault(user => user.X == x && user.Y == y) is { } user)
            {
                await run.Room.CollideAsync(user, item);
                if (box.Flag(StopOnCollision))
                    continue;
            }
            else if (await run.MoveAsync(item, x, y, item.Rotation))
            {
                box.FurniDirections[item.Item.Id] = direction;
                continue;
            }

            box.FurniDirections[item.Item.Id] = box.IntParam(WhenBlocked) switch
            {
                Wait => direction,
                Right45 => (direction + 1) % 8,
                Right90 => (direction + 2) % 8,
                Left45 => (direction + 7) % 8,
                Left90 => (direction + 6) % 8,
                Back => (direction + 4) % 8,
                _ => run.Room.Random.Next(8),
            };
        }
    }
}
