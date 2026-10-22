using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Session;

/// <summary>3981 Quit — the user leaves the room (to the hotel view, or on their way elsewhere).</summary>
[Incoming(ClientMessages.QuitMessageEvent)]
public sealed partial record QuitMessageEvent;
