using Habbo.Domain.Users;
using Habbo.Protocol.Messages.Incoming.Preferences;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Gateway.Messages.Handlers.Preferences;

/// <summary>SetOnlineIndicatorPreference — [preference]: who sees the player online.</summary>
internal sealed class SetOnlineIndicatorPreferenceHandler(FriendPresenceNotifier friendPresence)
    : IMessageHandler<SetOnlineIndicatorPreferenceMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, SetOnlineIndicatorPreferenceMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.UpdatePreferencesAsync(
            new PreferencesUpdate(OnlineIndicator: (OnlineIndicatorPreference)message.Preference));

        // the friends who may now see the player, or no longer may, are told
        if (context.Session.Player is { } player)
            await friendPresence.AnnounceAsync(player.Id, online: true, ct);
        return true;
    }
}
