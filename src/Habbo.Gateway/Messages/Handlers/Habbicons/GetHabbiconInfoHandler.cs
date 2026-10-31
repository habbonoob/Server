using Habbo.Protocol.Messages.Incoming.Habbicons;
using Habbo.Silo.Abstractions.Habbicons;

namespace Habbo.Gateway.Messages.Handlers.Habbicons;

/// <summary>GetHabbiconInfoMessageEvent — one Habbicon, as the player stands with it.</summary>
internal sealed class GetHabbiconInfoHandler(IHabbiconService habbicons) : IMessageHandler<GetHabbiconInfoMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetHabbiconInfoMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await habbicons.SendInfoAsync(player, message.HabbiconId);
        return true;
    }
}
