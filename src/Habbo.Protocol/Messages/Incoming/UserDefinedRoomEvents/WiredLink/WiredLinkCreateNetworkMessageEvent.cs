using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredLink;

/// <summary>WiredLinkCreateNetwork (October 2026) — a new network, from the dialog of the Link (item) it was made
/// in.</summary>
[Incoming(ClientMessages.WiredLinkCreateNetworkMessageEvent)]
public sealed partial record WiredLinkCreateNetworkMessageEvent(
    int ItemId,
    string Name);
