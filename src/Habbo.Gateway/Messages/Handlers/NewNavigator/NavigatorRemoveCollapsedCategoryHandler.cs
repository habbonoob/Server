using Habbo.Protocol.Messages.Incoming.NewNavigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.NewNavigator;

/// <summary>NavigatorRemoveCollapsedCategoryMessageEvent — unfold a result block.</summary>
internal sealed class NavigatorRemoveCollapsedCategoryHandler(INewNavigatorService service) : IMessageHandler<NavigatorRemoveCollapsedCategoryMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, NavigatorRemoveCollapsedCategoryMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SetCategoryCollapsedAsync(player, message.Category, collapsed: false);
        return true;
    }
}
