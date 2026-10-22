using Habbo.Protocol.Wire;
using Habbo.Protocol.Messages.Outgoing.Navigator;

namespace Habbo.Protocol.Messages.Outgoing.NewNavigator;

/// <summary>One result block: a heading and the rooms under it.</summary>
[WireStruct]
public sealed partial record NavigatorSearchBlockData(
    string SearchCode,
    string Text,
    int ActionAllowed,
    bool ForceClosed,
    int ViewMode,
    IReadOnlyList<GuestRoomData> Rooms);
