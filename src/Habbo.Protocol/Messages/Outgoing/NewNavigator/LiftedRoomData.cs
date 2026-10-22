using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.NewNavigator;

/// <summary>LiftedRoomData — a room lifted to the navigator's top strip</summary>
[WireStruct]
public sealed partial record LiftedRoomData(int FlatId, int AreaId, string Image, string Caption);
