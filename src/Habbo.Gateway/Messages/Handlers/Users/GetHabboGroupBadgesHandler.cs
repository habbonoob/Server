using Habbo.Protocol.Messages.Incoming.Users;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>GetHabboGroupBadgesMessageEvent — the badges of the groups the player meets in the room.</summary>
internal sealed class GetHabboGroupBadgesHandler : IMessageHandler<GetHabboGroupBadgesMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetHabboGroupBadgesMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SendGroupBadgesAsync(player.Id);
        return true;
    }
}
