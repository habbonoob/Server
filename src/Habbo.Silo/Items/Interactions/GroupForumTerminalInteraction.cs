using Habbo.Silo.Abstractions.Permissions;
using Habbo.Infrastructure;
using Habbo.Silo.Permissions;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Items.Interactions;

/// <summary>A group's forum terminal: group furniture that opens the group's forum (the client links to
/// groupforum/&lt;id&gt; itself). It is bought for a group that has a forum, or by the group's owner for a group without
/// one, which the purchase gives its forum; staff may buy one for any group — as the shop's forum selector offers them.</summary>
[FurniInteraction("furniture_group_forum_terminal")]
public sealed class GroupForumTerminalInteraction : GroupFurniInteraction
{
    /// <summary>The client opens the forum on its own; nothing changes on the server.</summary>
    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Nobody;

    public override async Task<bool> AcceptsOrderAsync(AppDbContext db, Guid buyerId, string clientParam)
    {
        if (GroupOf(clientParam) is not { } groupId)
            return false;

        var group = await db.Groups.AsNoTracking().Where(g => g.Id == groupId).Select(g => new { g.HasForum, g.OwnerId }).FirstOrDefaultAsync();
        if (group is null)
            return false;

        return group.HasForum || group.OwnerId == buyerId
            || await PermissionQuery.HasAsync(db, buyerId, PermissionNames.GroupForumTerminalAny);
    }

    /// <summary>The terminal brings the group its forum, if it had none.</summary>
    public override void Grant(FurniGrant grant)
    {
        base.Grant(grant);

        // saved with the purchase, so a purchase that fails gives no forum
        if (grant.Item.GroupId is { } groupId && grant.Db.Groups.Find(groupId) is { HasForum: false } group)
            group.HasForum = true;
    }

    public override Task UseAsync(FurniUse use) => Task.CompletedTask;
}
