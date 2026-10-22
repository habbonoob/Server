namespace Habbo.Silo.Wired.Effects;

/// <summary>Moves each of its users one tile in a direction, and turns them to a direction or by an
/// eighth of a turn, as the dialog chooses. A user who cannot go there stays.</summary>
[WiredBox("wf_act_move_rotate_user")]
public sealed class MoveUserEffect : WiredEffect
{
    private const int Movement = 0;
    private const int Rotation = 1;

    /// <summary>"No movement" and "no rotation"; 0 to 7 are the directions.</summary>
    private const int None = -1;
    private const int LastDirection = 7;
    private const int Clockwise = 9;
    private const int Anticlockwise = 10;
    private const int Directions = 8;

    public override int Code => EffectCodes.MoveUser;
    public override bool TargetsUsers => true;
    public override IReadOnlyList<int> DefaultIntParams => [None, None];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override Task RunAsync(WiredBox box, WiredRun run)
    {
        var movement = box.IntParam(Movement, None);
        var rotation = box.IntParam(Rotation, None);
        foreach (var user in run.Users(box).ToList())
        {
            var turned = rotation switch
            {
                >= 0 and <= LastDirection => rotation,
                Clockwise => (user.BodyDirection + 1) % Directions,
                Anticlockwise => (user.BodyDirection + Directions - 1) % Directions,
                _ => user.BodyDirection,
            };
            if (turned != user.BodyDirection)
                run.Room.TurnUser(user, turned);

            if (movement is >= 0 and <= LastDirection)
            {
                var (x, y) = WiredFurni.Step(user.X, user.Y, movement);
                run.Room.SlideUser(user, x, y, run.MoveOptions().AnimationMilliseconds, run.MoveOptions().CurveStrength);
            }
        }

        return Task.CompletedTask;
    }
}
