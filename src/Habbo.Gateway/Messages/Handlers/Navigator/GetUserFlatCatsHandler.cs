using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Protocol.Messages.Outgoing.Navigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>1258 GetUserFlatCats — the legacy navigator's room categories.</summary>
internal sealed class GetUserFlatCatsHandler(INavigatorCatalog catalog)
    : IMessageHandler<GetUserFlatCatsMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, GetUserFlatCatsMessageEvent message, CancellationToken ct)
    {
        await context.SendAsync(
            UserFlatCatsComposer.From(await catalog.GetFlatCategoriesAsync(ct)), ct);
        return true;
    }
}
