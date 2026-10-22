using System.Globalization;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Furni;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Items.Interactions;

/// <summary>Group furniture (furnidata special type 17): bought for one of the buyer's groups (the shop's group
/// selector sends its id), it carries the group's badge and colours, which the client reads from its string data
/// [state, group id, badge, colour 1, colour 2]. The badge and colours are the group's as they are now, filled in
/// whenever the item's data is read (<see cref="GroupFurniData"/>). It steps through its states on use, as other
/// multi-state furniture does.</summary>
[FurniInteraction("furniture_guild_customized")]
public class GroupFurniInteraction : FurniInteraction
{
    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Controllers;

    public override async Task<bool> AcceptsOrderAsync(AppDbContext db, Guid buyerId, string clientParam) =>
        GroupOf(clientParam) is { } groupId && await db.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == buyerId);

    /// <summary>One handed over any other way — a prize, a gift unwrapped — is of no group.</summary>
    public override void Grant(FurniGrant grant)
    {
        var groupId = GroupOf(grant.ClientParam);
        grant.Item.GroupId = groupId;
        FurniDataStore.Attach(grant.Db, grant.Item, GroupFurniData.Of(GroupFurniData.FirstState, groupId));
    }

    public override Task UseAsync(FurniUse use)
    {
        var data = use.Item.Data;
        if (use.Action != FurniAction.Use || data.StringValues is not { Count: > 0 } values)
            return Task.CompletedTask;

        var states = use.Item.Item.Definition!.States.Select(s => s.State).Where(state => state >= 0).Order().ToList();
        if (states.Count < 2)
            return Task.CompletedTask;

        var current = int.TryParse(values[GroupFurniData.State], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
        var next = states[(states.IndexOf(current) + 1) % states.Count];
        return use.Room.UpdateAsync(use.Item, GroupFurniData.WithState(data, next.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>The group the shop's selector sent, if it sent one.</summary>
    protected static int? GroupOf(string clientParam) =>
        int.TryParse(clientParam, NumberStyles.Integer, CultureInfo.InvariantCulture, out var groupId) && groupId > 0 ? groupId : null;
}
