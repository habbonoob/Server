using Habbo.Protocol.Messages.Incoming.Campaign;
using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Gateway.Messages.Handlers.Campaign;

/// <summary>OpenCampaignCalendarDoorAsStaffEvent — open any calendar door, as staff.</summary>
internal sealed class OpenCampaignCalendarDoorAsStaffHandler(ICampaignCalendarService service) : IMessageHandler<OpenCampaignCalendarDoorAsStaffEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, OpenCampaignCalendarDoorAsStaffEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.OpenDoorAsync(player, message.CampaignName, message.Day, asStaff: true);
        return true;
    }
}
