using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>RoomThumbnailUpdateResultComposer</summary>
[Message(ServerMessages.RoomThumbnailUpdateResultComposer)]
public sealed partial record RoomThumbnailUpdateResultComposer(int FlatId, int ResultCode)
{
    public const int Updated = 0;

    public const int NotAllowed = 1;
}
