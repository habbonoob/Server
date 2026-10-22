using Habbo.Protocol.Messages.Incoming.Game.Lobby;

namespace Habbo.Gateway.Messages.Handlers.Game.Lobby;

/// <summary>ResetResolutionAchievementMessageEvent — a resolution statue's challenge given up.</summary>
internal sealed class ResetResolutionAchievementHandler : IMessageHandler<ResetResolutionAchievementMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ResetResolutionAchievementMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.ResetResolutionAsync(player.Id, message.StuffId);
        return true;
    }
}
