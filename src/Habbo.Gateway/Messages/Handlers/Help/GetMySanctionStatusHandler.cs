using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>GetMySanctionStatusMessageEvent — the player's sanction record.</summary>
internal sealed class GetMySanctionStatusHandler(IModerationService service) : IMessageHandler<GetMySanctionStatusMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetMySanctionStatusMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendMySanctionStatusAsync(player.Id);
        return true;
    }
}
