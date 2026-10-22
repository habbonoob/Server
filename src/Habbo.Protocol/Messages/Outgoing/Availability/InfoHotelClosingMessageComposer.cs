using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Availability;

/// <summary>InfoHotelClosingMessageComposer — the hotel closes in so many minutes</summary>
[Message(ServerMessages.InfoHotelClosingMessageComposer)]
public sealed partial record InfoHotelClosingMessageComposer(int MinutesUntilClosing);
