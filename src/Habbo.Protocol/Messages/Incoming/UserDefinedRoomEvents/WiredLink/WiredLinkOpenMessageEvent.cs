using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredLink;

/// <summary>WiredLinkOpen (October 2026) — a WIRED Link's owner asks for its dialog, answering the room's
/// WiredLinkOpenMessageComposer.</summary>
[Incoming(ClientMessages.WiredLinkOpenMessageEvent)]
public sealed partial record WiredLinkOpenMessageEvent(
    int ItemId);
