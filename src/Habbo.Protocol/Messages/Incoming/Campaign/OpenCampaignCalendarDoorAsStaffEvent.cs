using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Campaign;

/// <summary>OpenCampaignCalendarDoorAsStaffEvent — open any door of the campaign calendar, as staff</summary>
[Incoming(ClientMessages.OpenCampaignCalendarDoorAsStaffEvent)]
public sealed partial record OpenCampaignCalendarDoorAsStaffEvent(string CampaignName, int Day);
