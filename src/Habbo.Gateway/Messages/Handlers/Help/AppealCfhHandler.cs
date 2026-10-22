using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>AppealCfhMessageEvent — appeal how a report was closed.</summary>
internal sealed class AppealCfhHandler(IModerationService service) : IMessageHandler<AppealCfhMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, AppealCfhMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.AppealAsync(player.Id, message.CallId);
        return true;
    }
}
