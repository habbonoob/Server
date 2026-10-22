using Habbo.Protocol.Messages.Incoming.NewNavigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.NewNavigator;

/// <summary>NavigatorAddCollapsedCategoryMessageEvent — fold a result block.</summary>
internal sealed class NavigatorAddCollapsedCategoryHandler(INewNavigatorService service) : IMessageHandler<NavigatorAddCollapsedCategoryMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, NavigatorAddCollapsedCategoryMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SetCategoryCollapsedAsync(player, message.Category, collapsed: true);
        return true;
    }
}
