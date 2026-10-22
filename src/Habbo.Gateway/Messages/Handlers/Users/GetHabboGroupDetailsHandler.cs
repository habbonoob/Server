using Habbo.Protocol.Messages.Incoming.Users;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>GetHabboGroupDetailsMessageEvent — a group's details.</summary>
internal sealed class GetHabboGroupDetailsHandler(IGroupService service) : IMessageHandler<GetHabboGroupDetailsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetHabboGroupDetailsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendDetailsAsync(player.Id, message.GroupId, message.OpenDetails);
        return true;
    }
}
