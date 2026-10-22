using Habbo.Protocol.Messages.Incoming.Users;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>GetBadgeLeaderboard — [type, rarity, page, pageSize]: a page of a badge leaderboard.</summary>
internal sealed class GetBadgeLeaderboardHandler : IMessageHandler<GetBadgeLeaderboardMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetBadgeLeaderboardMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.SendBadgeLeaderboardAsync(message.Type, message.Rarity, message.Page, message.PageSize);
        return true;
    }
}
