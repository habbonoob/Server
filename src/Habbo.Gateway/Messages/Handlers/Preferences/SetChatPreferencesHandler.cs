using Habbo.Protocol.Messages.Incoming.Preferences;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Gateway.Messages.Handlers.Preferences;

/// <summary>1407 SetChatPreferences — [preferOldChat]: turns the free-flow chat off. September 2026 sends the
/// player's chat mode, bubble width and scroll speed after it, and false in its place: it has no old chat.</summary>
internal sealed class SetChatPreferencesHandler : IMessageHandler<SetChatPreferencesMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, SetChatPreferencesMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.UpdatePreferencesAsync(message.ChatMode is null
            ? new PreferencesUpdate(FreeFlowChatDisabled: message.PreferOldChat)
            : new PreferencesUpdate(ChatMode: message.ChatMode, ChatBubbleWidth: message.ChatBubbleWidth,
                ChatScrollSpeed: message.ChatScrollSpeed));
        return true;
    }
}
