using Habbo.Protocol.Messages.Incoming.NewNavigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.NewNavigator;

/// <summary>NavigatorAddSavedSearchEvent — save a search to the side pane.</summary>
internal sealed class NavigatorAddSavedSearchHandler(INewNavigatorService service) : IMessageHandler<NavigatorAddSavedSearchEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, NavigatorAddSavedSearchEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.AddSavedSearchAsync(player, message.SearchCode, message.Filter);
        return true;
    }
}
