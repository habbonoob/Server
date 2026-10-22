using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Session;

/// <summary>1584 CloseConnectionMessageComposer — the room is closed to the user: the client leaves it. It has no body.</summary>
[Message(ServerMessages.CloseConnectionMessageComposer)]
public sealed partial record CloseConnectionMessageComposer;
