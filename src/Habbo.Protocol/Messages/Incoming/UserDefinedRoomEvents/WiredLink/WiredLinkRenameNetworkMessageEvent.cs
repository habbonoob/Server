using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredLink;

/// <summary>WiredLinkRenameNetwork (October 2026) — a network's new name, from a Link's dialog.</summary>
[Incoming(ClientMessages.WiredLinkRenameNetworkMessageEvent)]
public sealed partial record WiredLinkRenameNetworkMessageEvent(
    int ItemId,
    string NetworkId,
    string Name);
