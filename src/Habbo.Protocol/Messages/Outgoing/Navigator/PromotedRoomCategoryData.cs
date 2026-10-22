using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>PromotedRoomCategoryData — a promoted category: its leading room first</summary>
[WireStruct]
public sealed partial record PromotedRoomCategoryData(string Code, string LeaderFigure, IReadOnlyList<GuestRoomData> Rooms);
