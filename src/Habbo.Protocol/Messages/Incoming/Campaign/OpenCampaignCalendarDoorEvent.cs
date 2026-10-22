using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Campaign;

/// <summary>OpenCampaignCalendarDoorEvent — open a door of the campaign calendar</summary>
[Incoming(ClientMessages.OpenCampaignCalendarDoorEvent)]
public sealed partial record OpenCampaignCalendarDoorEvent(string CampaignName, int Day);
