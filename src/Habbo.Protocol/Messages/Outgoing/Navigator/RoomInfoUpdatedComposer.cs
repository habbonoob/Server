using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>RoomInfoUpdatedComposer</summary>
[Message(ServerMessages.RoomInfoUpdatedComposer)]
public sealed partial record RoomInfoUpdatedComposer(int FlatId);
