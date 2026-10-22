using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Campaign;

/// <summary>CampaignCalendarDoorOpenedMessageComposer — what was behind a door, if it opened</summary>
[Message(ServerMessages.CampaignCalendarDoorOpenedMessageComposer)]
public sealed partial record CampaignCalendarDoorOpenedMessageComposer(bool DoorOpened, string ProductName, string CustomImage, string FurnitureClassName);
