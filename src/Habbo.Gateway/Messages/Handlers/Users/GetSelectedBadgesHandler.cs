using Habbo.Protocol.Messages.Incoming.Users;

using Habbo.Protocol.Messages.Outgoing.Users;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>GetSelectedBadgesMessageEvent — the badges a user wears, for their info stand.</summary>
internal sealed class GetSelectedBadgesHandler : IMessageHandler<GetSelectedBadgesMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetSelectedBadgesMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        var worn = await playerGrain.GetWornBadgesAsync(message.UserId);
        await context.SendAsync(HabboUserBadgesMessageComposer.From(message.UserId, worn), ct);
        return true;
    }
}
