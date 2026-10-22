using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.NewNavigator;

/// <summary>NavigatorLiftedRoomsComposer</summary>
[Message(ServerMessages.NavigatorLiftedRoomsComposer)]
public sealed partial record NavigatorLiftedRoomsComposer(IReadOnlyList<LiftedRoomData> Rooms);
