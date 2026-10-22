using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>The group a room belongs to. Present only when GuestRoomData sets bit 0x02.</summary>
[WireStruct]
public sealed partial record HabboGroupInfo(int GroupId, string GroupName, string BadgeCode);
