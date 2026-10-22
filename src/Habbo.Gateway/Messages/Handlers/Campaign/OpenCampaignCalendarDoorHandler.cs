using Habbo.Protocol.Messages.Incoming.Campaign;
using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Gateway.Messages.Handlers.Campaign;

/// <summary>OpenCampaignCalendarDoorEvent — open a calendar door.</summary>
internal sealed class OpenCampaignCalendarDoorHandler(ICampaignCalendarService service) : IMessageHandler<OpenCampaignCalendarDoorEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, OpenCampaignCalendarDoorEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.OpenDoorAsync(player, message.CampaignName, message.Day, asStaff: false);
        return true;
    }
}
