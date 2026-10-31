using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredLink;

/// <summary>WiredLinkSaveConfiguration (October 2026) — a Link's network ("0": none) and whether it receives what its
/// own room sends.</summary>
[Incoming(ClientMessages.WiredLinkSaveConfigurationMessageEvent)]
public sealed partial record WiredLinkSaveConfigurationMessageEvent(
    int ItemId,
    string NetworkId,
    bool ReceiveLocal);
