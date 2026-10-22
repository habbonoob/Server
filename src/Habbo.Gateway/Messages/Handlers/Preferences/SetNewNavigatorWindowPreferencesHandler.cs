using Habbo.Protocol.Messages.Incoming.Preferences;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Preferences;

/// <summary>SetNewNavigatorWindowPreferencesMessageEvent — where the navigator window sits.</summary>
internal sealed class SetNewNavigatorWindowPreferencesHandler(INewNavigatorService service) : IMessageHandler<SetNewNavigatorWindowPreferencesMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SetNewNavigatorWindowPreferencesMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SetWindowPreferencesAsync(player, message.X, message.Y, message.Width, message.Height, message.LeftPanelHidden, message.ResultsMode);
        return true;
    }
}
