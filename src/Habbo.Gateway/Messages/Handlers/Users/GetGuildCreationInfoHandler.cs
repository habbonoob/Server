using Habbo.Protocol.Messages.Incoming.Users;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>GetGuildCreationInfoMessageEvent — the price of a group and the rooms it could be made for.</summary>
internal sealed class GetGuildCreationInfoHandler(IGroupService service) : IMessageHandler<GetGuildCreationInfoMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetGuildCreationInfoMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendCreationInfoAsync(player.Id);
        return true;
    }
}
