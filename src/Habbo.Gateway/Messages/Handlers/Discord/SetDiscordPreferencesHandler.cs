using Habbo.Protocol.Messages.Incoming.Discord;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Gateway.Messages.Handlers.Discord;

/// <summary>SetDiscordPreferencesMessageEvent — change Discord preferences.</summary>
internal sealed class SetDiscordPreferencesHandler(IPlayerSettingsService service) : IMessageHandler<SetDiscordPreferencesMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SetDiscordPreferencesMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SetDiscordPreferencesAsync(player, message.Version, message.ShowHabbo, message.ShareActivity, message.HideInHiddenRooms, message.AllowJoining);
        return true;
    }
}
