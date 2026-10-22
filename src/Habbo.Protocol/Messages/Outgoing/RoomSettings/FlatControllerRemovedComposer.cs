using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.RoomSettings;

/// <summary>2634 FlatControllerRemovedComposer — someone's rights in the room were taken back.</summary>
[Message(ServerMessages.FlatControllerRemovedComposer)]
public sealed partial record FlatControllerRemovedComposer(int RoomId, int UserId);
