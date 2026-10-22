namespace Habbo.Silo.Wired.Effects;

/// <summary>Moves its furni together to a target location (a furni, or by default a user), moved by the offsets, keeping
/// the distances between them ("the distance between the moving furniture will stay consistent", Habbo's text). The
/// group's top left corner lands on the target; which point of the group Habbo puts there was not measured. Each furni
/// moves on its own, so one that cannot stand where it would go stays: Habbo recommends checking with "Can Perform
/// Movements" first, which this plan answers.</summary>
[WiredBox("wf_act_move_furni_as_group")]
public sealed class MoveAsGroupEffect : WiredMoveEffect
{
    private const int TargetIsUser = 0;
    private const int XOffset = 1;
    private const int YOffset = 2;

    private const int Moving = 0;
    private const int TargetFurni = 1;
    private const int TargetUsers = 0;

    public override int Code => EffectCodes.MoveAsGroup;
    public override bool AllowsWallFurni => true;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.EitherPicks, WiredSlot.EitherPicks.DefaultingTo(WiredSources.TriggerFurni)];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Users];
    public override IReadOnlyList<int> DefaultIntParams => [1, 0, 0];

    protected override IReadOnlyList<WiredMove> Plan(WiredBox box, WiredRun run)
    {
        var group = run.Furni(box, Moving).Where(item => item.Item.Definition!.Type == Habbo.Domain.Furniture.ItemType.Floor).ToList();
        (int X, int Y)? target = box.Flag(TargetIsUser)
            ? run.Users(box, TargetUsers).Select(user => ((int, int)?)(user.X, user.Y)).FirstOrDefault()
            : run.Furni(box, TargetFurni).Select(item => ((int, int)?)(item.X, item.Y)).FirstOrDefault();
        if (group.Count == 0 || target is not { } to)
            return [];

        var dx = to.X + box.IntParam(XOffset) - group.Min(item => item.X);
        var dy = to.Y + box.IntParam(YOffset) - group.Min(item => item.Y);
        if (dx == 0 && dy == 0)
            return [];

        // the furni furthest along the way go first, so none is stopped by one of its own group still standing there
        return group.OrderByDescending(item => item.X * Math.Sign(dx) + item.Y * Math.Sign(dy))
            .Select(item => new WiredMove(item, item.X + dx, item.Y + dy, item.Rotation))
            .ToList();
    }
}
