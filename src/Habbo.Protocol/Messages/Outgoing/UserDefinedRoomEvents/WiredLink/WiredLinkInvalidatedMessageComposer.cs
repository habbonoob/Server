using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredLink;

/// <summary>WiredLinkInvalidated (October 2026) — a network was deleted: the Link's open dialog drops it, and the Link
/// is disconnected from it.</summary>
[Message(ServerMessages.WiredLinkInvalidatedMessageComposer)]
public sealed partial record WiredLinkInvalidatedMessageComposer(
    int RoomId,
    int ItemId,
    string DeletedNetworkId);
