using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.RoomSettings;

/// <summary>2933 NoSuchFlatComposer — the room the client asked for is gone.</summary>
[Message(ServerMessages.NoSuchFlatComposer)]
public sealed partial record NoSuchFlatComposer(int RoomId);
