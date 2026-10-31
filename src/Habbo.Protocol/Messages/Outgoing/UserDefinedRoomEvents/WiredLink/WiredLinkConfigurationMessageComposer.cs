using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredLink;

/// <summary>WiredLinkConfiguration (October 2026) — a Link's dialog: the room and the Link, the network it is
/// connected to ("0": none), whether it receives its own room's messages, and every network of its owner.</summary>
[Message(ServerMessages.WiredLinkConfigurationMessageComposer)]
public sealed partial record WiredLinkConfigurationMessageComposer(
    int RoomId,
    int ItemId,
    string SelectedNetworkId,
    bool ReceiveLocal,
    IReadOnlyList<WiredLinkNetworkData> Networks);
