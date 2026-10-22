using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Availability;

/// <summary>LoginFailedHotelClosedMessageComposer — the hotel is closed, and opens again at this time</summary>
[Message(ServerMessages.LoginFailedHotelClosedMessageComposer)]
public sealed partial record LoginFailedHotelClosedMessageComposer(int OpenHour, int OpenMinute);
