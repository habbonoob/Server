using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredLink;

/// <summary>WiredLinkGetConnections (October 2026) — the rooms a network's Links stand in, for a Link's dialog.</summary>
[Incoming(ClientMessages.WiredLinkGetConnectionsMessageEvent)]
public sealed partial record WiredLinkGetConnectionsMessageEvent(
    int ItemId,
    string NetworkId);
