using Habbo.Protocol.Messages.Incoming.Room.Furniture;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>GetGuildFurniContextMenuInfoMessageEvent — a group furni's context menu.</summary>
internal sealed class GetGuildFurniContextMenuInfoHandler(IGroupForumService service) : IMessageHandler<GetGuildFurniContextMenuInfoMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetGuildFurniContextMenuInfoMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendFurniContextMenuInfoAsync(player.Id, message.ItemId, message.GroupId);
        return true;
    }
}
