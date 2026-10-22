using Habbo.Domain.Furniture;

namespace Habbo.Silo.Wired.Effects;

/// <summary>Moves each of its users onto a target furni (one at random). A user who was walking
/// keeps walking if that brought them closer to where they were going, always, or never, as the
/// dialog chooses.</summary>
[WiredBox("wf_act_user_to_furni")]
public sealed class MoveUserToFurniEffect : WiredEffect
{
    // user_move.walkmode.0 to .2
    private const int KeepWalkingIfCloser = 0;
    private const int KeepWalking = 1;

    public override int Code => EffectCodes.MoveUserToFurni;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.Furni];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Users];
    public override IReadOnlyList<int> DefaultIntParams => [KeepWalkingIfCloser];

    public override Task RunAsync(WiredBox box, WiredRun run)
    {
        // floor furni only: a wall item stands at no tile a user could be put on
        var targets = run.Furni(box).Where(t => t.Item.Definition?.Type == ItemType.Floor).ToList();
        if (targets.Count == 0)
            return Task.CompletedTask;

        foreach (var user in run.Users(box).ToList())
        {
            var target = targets[run.Room.Random.Next(targets.Count)];
            var goal = user.Goal;
            var keep = goal is { } g && box.IntParam(0) switch
            {
                KeepWalking => true,
                KeepWalkingIfCloser => Distance(target.X, target.Y, g) < Distance(user.X, user.Y, g),
                _ => false,
            };
            run.Room.WarpUser(user, target.X, target.Y, keep ? goal : null);
        }

        return Task.CompletedTask;
    }

    private static int Distance(int x, int y, (int X, int Y) goal) => Math.Max(Math.Abs(goal.X - x), Math.Abs(goal.Y - y));
}
