using Habbo.Protocol.Messages.Incoming.Game.Lobby;

namespace Habbo.Gateway.Messages.Handlers.Game.Lobby;

/// <summary>GetResolutionAchievementsMessageEvent — a resolution statue opened, or its achievement picked.</summary>
internal sealed class GetResolutionAchievementsHandler : IMessageHandler<GetResolutionAchievementsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetResolutionAchievementsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.OpenResolutionAsync(player.Id, message.StuffId, message.AchievementId);
        return true;
    }
}
