using Habbo.Protocol.Messages.Incoming.Moderator;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Moderator;

/// <summary>ModToolPreferencesEvent — where the mod tool sits.</summary>
internal sealed class ModToolPreferencesHandler(IModerationService service) : IMessageHandler<ModToolPreferencesEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ModToolPreferencesEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SavePreferencesAsync(player.Id, message.WindowX, message.WindowY, message.WindowWidth, message.WindowHeight);
        return true;
    }
}
