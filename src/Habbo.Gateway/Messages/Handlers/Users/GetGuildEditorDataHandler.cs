using Habbo.Protocol.Messages.Incoming.Users;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>GetGuildEditorDataMessageEvent — the badge parts and colours the editor offers.</summary>
internal sealed class GetGuildEditorDataHandler(IGroupService service) : IMessageHandler<GetGuildEditorDataMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetGuildEditorDataMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendEditorDataAsync(player.Id);
        return true;
    }
}
