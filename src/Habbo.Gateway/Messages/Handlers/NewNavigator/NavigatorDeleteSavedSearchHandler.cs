using Habbo.Protocol.Messages.Incoming.NewNavigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.NewNavigator;

/// <summary>NavigatorDeleteSavedSearchEvent — forget a saved search.</summary>
internal sealed class NavigatorDeleteSavedSearchHandler(INewNavigatorService service) : IMessageHandler<NavigatorDeleteSavedSearchEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, NavigatorDeleteSavedSearchEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.DeleteSavedSearchAsync(player, message.SearchId);
        return true;
    }
}
