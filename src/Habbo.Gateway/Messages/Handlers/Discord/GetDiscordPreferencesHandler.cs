using Habbo.Protocol.Messages.Incoming.Discord;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Gateway.Messages.Handlers.Discord;

/// <summary>GetDiscordPreferencesMessageEvent — Discord preferences.</summary>
internal sealed class GetDiscordPreferencesHandler(IPlayerSettingsService service) : IMessageHandler<GetDiscordPreferencesMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetDiscordPreferencesMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendDiscordPreferencesAsync(player);
        return true;
    }
}
