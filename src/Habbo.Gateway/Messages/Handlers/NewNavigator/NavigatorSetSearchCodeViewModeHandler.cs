using Habbo.Protocol.Messages.Incoming.NewNavigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.NewNavigator;

/// <summary>NavigatorSetSearchCodeViewModeMessageEvent — show a search's results as a list or thumbnails.</summary>
internal sealed class NavigatorSetSearchCodeViewModeHandler(INewNavigatorService service) : IMessageHandler<NavigatorSetSearchCodeViewModeMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, NavigatorSetSearchCodeViewModeMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SetViewModeAsync(player, message.SearchCode, message.ViewMode);
        return true;
    }
}
