using Habbo.Protocol.Messages.Incoming.UserClassification;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Gateway.Messages.Handlers.UserClassification;

/// <summary>PeerUsersClassificationMessageEvent — the users of a class across the hotel.</summary>
internal sealed class PeerUsersClassificationHandler(IUserClassificationService service) : IMessageHandler<PeerUsersClassificationMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, PeerUsersClassificationMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendHotelAsync(player, message.Classification);
        return true;
    }
}
