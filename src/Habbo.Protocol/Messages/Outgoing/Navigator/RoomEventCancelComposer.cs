using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>RoomEventCancelComposer — the room's promotion ended; the client drops its event bar. It has no body.</summary>
[Message(ServerMessages.RoomEventCancelComposer)]
public sealed partial record RoomEventCancelComposer;
