using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>CanCreateRoomComposer</summary>
[Message(ServerMessages.CanCreateRoomComposer)]
public sealed partial record CanCreateRoomComposer(int ResultCode, int RoomLimit)
{
    public const int Allowed = 0;

    /// <summary>The player owns as many rooms as they may; the client names the limit.</summary>
    public const int LimitReached = 1;
}
