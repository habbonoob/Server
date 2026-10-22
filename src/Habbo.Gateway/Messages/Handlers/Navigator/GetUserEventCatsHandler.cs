using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Protocol.Messages.Outgoing.Navigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>3786 GetUserEventCats — the promoted-event categories.</summary>
internal sealed class GetUserEventCatsHandler(INavigatorCatalog catalog)
    : IMessageHandler<GetUserEventCatsMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, GetUserEventCatsMessageEvent message, CancellationToken ct)
    {
        await context.SendAsync(
            UserEventCatsComposer.From(await catalog.GetEventCategoriesAsync(ct)), ct);
        return true;
    }
}
