using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.RoomSettings;

/// <summary>3568 FlatControllersComposer — everyone with rights in the room.</summary>
[Message(ServerMessages.FlatControllersComposer)]
public sealed partial record FlatControllersComposer(int RoomId, IReadOnlyList<FlatControllerData> Controllers);
