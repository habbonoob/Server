namespace Habbo.Silo.Items.Interactions;

/// <summary>A group gate: group furniture that lets only the group's members walk through ("Set it up so only members
/// of your Group can pass through!"). seed/furni_interactions.json names it for group furniture that is a gate.</summary>
[FurniInteraction(Name)]
public sealed class GroupGateInteraction : GroupFurniInteraction
{
    public const string Name = "group_gate";

    /// <summary>It opens for members on its own; nobody switches it.</summary>
    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Nobody;

    public override Task UseAsync(FurniUse use) => Task.CompletedTask;

    /// <summary>Whether an avatar in these groups may cross a gate of this group; a gate of no group lets nobody by.</summary>
    public static bool LetsThrough(int? gateGroupId, IReadOnlySet<int>? groupIds) =>
        gateGroupId is { } groupId && groupIds is not null && groupIds.Contains(groupId);
}
