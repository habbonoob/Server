using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>RoomEventComposer — a room's event, or its absence (an ad id of -1)</summary>
[Message(ServerMessages.RoomEventComposer)]
public sealed partial record RoomEventComposer(int AdId, int OwnerAvatarId, string OwnerAvatarName, int FlatId, int EventType, string EventName, string EventDescription, int MinutesSinceCreation, int MinutesUntilExpiration, int CategoryId)
{
    /// <summary>What the client reads as "no event": it drops the bar when the owner id is not positive.</summary>
    private const int None = -1;

    public static RoomEventComposer From(RoomEventInfo? roomEvent, int roomId, DateTimeOffset now) => roomEvent is null
        ? new(None, None, string.Empty, roomId, None, string.Empty, string.Empty, 0, 0, None)
        : new(roomEvent.Id, roomEvent.OwnerAccountId, roomEvent.OwnerName, roomEvent.RoomId, roomEvent.Type,
            roomEvent.Name, roomEvent.Description,
            (int)(now - roomEvent.CreatedAt).TotalMinutes,
            Math.Max(0, (int)Math.Ceiling((roomEvent.ExpiresAt - now).TotalMinutes)),
            roomEvent.CategoryId);
}
