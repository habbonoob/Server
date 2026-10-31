using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredLink;

/// <summary>WiredLinkOpen (October 2026) — the room tells a Link's owner, who used it, to open its dialog; the client
/// asks for it (WiredLinkOpenMessageEvent) if it owns the Link.</summary>
[Message(ServerMessages.WiredLinkOpenMessageComposer)]
public sealed partial record WiredLinkOpenMessageComposer(
    int ItemId);
