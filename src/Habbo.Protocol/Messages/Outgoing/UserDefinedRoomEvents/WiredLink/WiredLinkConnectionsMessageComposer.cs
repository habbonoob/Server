using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredLink;

/// <summary>WiredLinkConnections (October 2026) — the Links connected to a network, for the dialog of the Link (in the
/// room) it was asked from.</summary>
[Message(ServerMessages.WiredLinkConnectionsMessageComposer)]
public sealed partial record WiredLinkConnectionsMessageComposer(
    int RoomId,
    int ItemId,
    string NetworkId,
    IReadOnlyList<WiredLinkConnectionData> Connections);
