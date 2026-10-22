using Habbo.Protocol.Messages.Incoming.Preferences;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Gateway.Messages.Handlers.Preferences;

/// <summary>3271 SetIgnoreRoomInvites — [ignore].</summary>
internal sealed class SetIgnoreRoomInvitesHandler : IMessageHandler<SetIgnoreRoomInvitesMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, SetIgnoreRoomInvitesMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.UpdatePreferencesAsync(new PreferencesUpdate(RoomInvitesIgnored: message.Ignore));
        return true;
    }
}
