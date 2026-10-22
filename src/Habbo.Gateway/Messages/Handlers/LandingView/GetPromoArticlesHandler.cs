using Habbo.Protocol.Messages.Incoming.LandingView;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.LandingView;

/// <summary>GetPromoArticlesMessageEvent — the landing view's promo articles.</summary>
internal sealed class GetPromoArticlesHandler(IPromoArticleService service) : IMessageHandler<GetPromoArticlesMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetPromoArticlesMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendAsync(player);
        return true;
    }
}
