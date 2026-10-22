using Habbo.Protocol.Messages.Incoming.GroupForums;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.GroupForums;

/// <summary>UpdateForumReadMarkerMessageEvent — how far the player has read.</summary>
internal sealed class UpdateForumReadMarkerHandler(IGroupForumService service) : IMessageHandler<UpdateForumReadMarkerMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, UpdateForumReadMarkerMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.MarkReadAsync(player.Id, message.Markers.Select(m => new ForumReadMark(m.GroupId, m.LastReadMessageIndex, m.Reset)).ToList());
        return true;
    }
}
