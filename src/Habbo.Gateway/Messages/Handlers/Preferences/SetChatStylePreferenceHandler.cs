using Habbo.Protocol.Messages.Incoming.Preferences;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Gateway.Messages.Handlers.Preferences;

/// <summary>3760 SetChatStylePreference — [styleId, fontSize since September 2026]: the bubble picked in the style
/// selector, and the chat's text size.</summary>
internal sealed class SetChatStylePreferenceHandler : IMessageHandler<SetChatStylePreferenceEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, SetChatStylePreferenceEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.UpdatePreferencesAsync(
            new PreferencesUpdate(PreferedChatStyle: message.StyleId, ChatFontSize: message.FontSize));
        return true;
    }
}
