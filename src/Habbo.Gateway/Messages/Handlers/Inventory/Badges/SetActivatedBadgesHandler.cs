using Habbo.Protocol.Messages.Incoming.Inventory.Badges;
using Habbo.Protocol.Messages.Outgoing.Users;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Quests;

namespace Habbo.Gateway.Messages.Handlers.Inventory.Badges;

/// <summary>SetActivatedBadgesEvent — wears badges; the room sees them, or the player alone when
/// not in one. A seasonal quest to wear one of them is done.</summary>
internal sealed class SetActivatedBadgesHandler(ISeasonalQuestService seasonalQuests) : IMessageHandler<SetActivatedBadgesEvent>
{
    /// <summary>The quest type wearing a badge counts towards (WEAR_BADGE, its badge code the parameter).</summary>
    private const string WearBadge = "WEAR_BADGE";

    private const int Worn = 1;

    public async Task<bool> HandleAsync(GatewaySessionContext context, SetActivatedBadgesEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.PlayerGrain is not { } playerGrain)
            return false;

        var worn = await playerGrain.SetWornBadgesAsync(
            message.Slots.Select(slot => new WornBadge(slot.Slot, slot.Code)).ToList());

        if (context.Session.Room is { } room)
            await room.ShowWornBadgesAsync(player.Id, worn);
        else
            await context.SendAsync(HabboUserBadgesMessageComposer.From(player.AccountId, worn), ct);

        foreach (var badge in worn)
            await seasonalQuests.ProgressAsync(player.Id, WearBadge, badge.Code, Worn);
        return true;
    }
}
