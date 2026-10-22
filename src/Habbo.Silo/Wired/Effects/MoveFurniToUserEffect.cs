namespace Habbo.Silo.Wired.Effects;

/// <summary>Moves the moving furni to where a target user (one at random) stands.</summary>
[WiredBox("wf_act_furni_to_user")]
public sealed class MoveFurniToUserEffect : WiredMoveEffect
{
    public override int Code => EffectCodes.MoveFurniToUser;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.Furni];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Users];

    /// <summary>It goes to where a user stands, so users cannot be in its way.</summary>
    public override bool AvatarsBlock => false;

    protected override IReadOnlyList<WiredMove> Plan(WiredBox box, WiredRun run)
    {
        var users = run.Users(box).ToList();
        if (users.Count == 0)
            return [];

        return run.Furni(box).Select(item =>
        {
            var user = users[run.Room.Random.Next(users.Count)];
            return new WiredMove(item, user.X, user.Y, item.Rotation);
        }).ToList();
    }
}
