using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Silo.Abstractions.Moderation;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>GuideSessionOnDutyUpdateMessageEvent — go on or off duty.</summary>
internal sealed class GuideSessionOnDutyUpdateHandler(IGrainFactory service) : IMessageHandler<GuideSessionOnDutyUpdateMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GuideSessionOnDutyUpdateMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.GetGrain<IGuideDeskGrain>(0).UpdateDutyAsync(player, message.OnDuty, message.HandleTours, message.HandleHelps, message.HandleChatReviews);
        return true;
    }
}
