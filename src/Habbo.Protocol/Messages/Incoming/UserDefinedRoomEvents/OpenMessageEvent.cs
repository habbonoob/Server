using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents;

/// <summary>1955 Open — the client asks for a wired item's settings, answering the server's OpenComposer.</summary>
[Incoming(ClientMessages.OpenMessageEvent)]
public sealed partial record OpenMessageEvent(
    int ItemId);
