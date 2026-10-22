using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>OfficialRoomsComposer</summary>
[Message(ServerMessages.OfficialRoomsComposer)]
public sealed partial record OfficialRoomsComposer(IReadOnlyList<OfficialRoomEntryData> Entries, IReadOnlyList<OfficialRoomEntryData> AdRoom, IReadOnlyList<PromotedRoomCategoryData> PromotedRooms);
