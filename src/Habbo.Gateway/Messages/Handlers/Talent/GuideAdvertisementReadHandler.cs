using Habbo.Protocol.Messages.Incoming.Talent;

namespace Habbo.Gateway.Messages.Handlers.Talent;

/// <summary>GuideAdvertisementReadMessageEvent — the player read what the guides do, which is what the guide
/// advertisement reader achievement counts.</summary>
internal sealed class GuideAdvertisementReadHandler : IMessageHandler<GuideAdvertisementReadMessageEvent>
{
    private const string ReaderAchievement = "ACH_GuideAdvertisementReader";

    public async Task<bool> HandleAsync(GatewaySessionContext context, GuideAdvertisementReadMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.ProgressAchievementAsync(ReaderAchievement, 1);
        return true;
    }
}
