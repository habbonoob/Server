using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.RoomSettings;

/// <summary>360 FlatControllerAddedComposer — someone was given rights in the room.</summary>
[Message(ServerMessages.FlatControllerAddedComposer)]
public sealed partial record FlatControllerAddedComposer(int RoomId, FlatControllerData Controller);
