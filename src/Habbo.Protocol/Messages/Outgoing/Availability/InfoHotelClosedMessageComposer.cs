using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Availability;

/// <summary>InfoHotelClosedMessageComposer — the hotel has closed, and opens again at this time</summary>
[Message(ServerMessages.InfoHotelClosedMessageComposer)]
public sealed partial record InfoHotelClosedMessageComposer(int OpenHour, int OpenMinute, bool UserThrownOutAtClose);
