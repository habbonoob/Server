using Habbo.Protocol.Messages.Incoming.Preferences;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Gateway.Messages.Handlers.Preferences;

/// <summary>SetUIFlagsMessageEvent — Remembers the client's UI flags.</summary>
internal sealed class SetUIFlagsHandler : IMessageHandler<SetUIFlagsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SetUIFlagsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.UpdatePreferencesAsync(new PreferencesUpdate(UiFlags: message.Flags));
        return true;
    }
}
