using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredLink;

/// <summary>WiredLinkDeleteNetwork (October 2026) — a network deleted from a Link's dialog.</summary>
[Incoming(ClientMessages.WiredLinkDeleteNetworkMessageEvent)]
public sealed partial record WiredLinkDeleteNetworkMessageEvent(
    int ItemId,
    string NetworkId);
