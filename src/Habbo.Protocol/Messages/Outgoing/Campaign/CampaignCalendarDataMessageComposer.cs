using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Campaign;

/// <summary>CampaignCalendarDataMessageComposer — the running campaign calendar and the player's doors</summary>
[Message(ServerMessages.CampaignCalendarDataMessageComposer)]
public sealed partial record CampaignCalendarDataMessageComposer(string CampaignName, string CampaignImage, int CurrentDay, int CampaignDays, IReadOnlyList<int> OpenedDays, IReadOnlyList<int> MissedDays);
