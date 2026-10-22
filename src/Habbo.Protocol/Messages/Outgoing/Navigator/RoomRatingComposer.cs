using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>RoomRatingComposer</summary>
[Message(ServerMessages.RoomRatingComposer)]
public sealed partial record RoomRatingComposer(int Rating, bool CanRate);
