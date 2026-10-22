using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>GuestRoomSearchResultComposer</summary>
[Message(ServerMessages.GuestRoomSearchResultComposer)]
public sealed partial record GuestRoomSearchResultComposer(int SearchType, string SearchParam, IReadOnlyList<GuestRoomData> Rooms, bool HasAd, [property: WireWhen("HasAd")] OfficialRoomEntryData? Ad);
