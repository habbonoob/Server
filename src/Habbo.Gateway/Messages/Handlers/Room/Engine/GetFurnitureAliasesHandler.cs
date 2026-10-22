using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Incoming.Room.Engine;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>1952 GetFurnitureAliases — the client resolves furniture from its own furnidata,
/// so no server-side aliases are in play.</summary>
internal sealed class GetFurnitureAliasesHandler : IMessageHandler<GetFurnitureAliasesMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, GetFurnitureAliasesMessageEvent message, CancellationToken ct)
    {
        await context.SendAsync(new FurnitureAliasesMessageComposer([]), ct);
        return true;
    }
}
