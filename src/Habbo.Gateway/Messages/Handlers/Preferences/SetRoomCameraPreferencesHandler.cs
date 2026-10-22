using Habbo.Protocol.Messages.Incoming.Preferences;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Gateway.Messages.Handlers.Preferences;

/// <summary>3187 SetRoomCameraPreferences — [disabled]: the "disable room camera follow" checkbox.</summary>
internal sealed class SetRoomCameraPreferencesHandler : IMessageHandler<SetRoomCameraPreferencesMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, SetRoomCameraPreferencesMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.UpdatePreferencesAsync(
            new PreferencesUpdate(RoomCameraFollowDisabled: message.FollowDisabled));
        return true;
    }
}
