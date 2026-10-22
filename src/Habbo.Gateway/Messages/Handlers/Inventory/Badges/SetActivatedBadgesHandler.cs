using Habbo.Protocol.Messages.Incoming.Inventory.Badges;
using Habbo.Protocol.Messages.Outgoing.Users;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Gateway.Messages.Handlers.Inventory.Badges;

/// <summary>SetActivatedBadgesEvent — wears badges; the room sees them, or the player alone when
/// not in one.</summary>
internal sealed class SetActivatedBadgesHandler : IMessageHandler<SetActivatedBadgesEvent>
{
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
        return true;
    }
}
