using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>HabboGroupDetailsMessageComposer</summary>
[Message(ServerMessages.HabboGroupDetailsMessageComposer)]
public sealed partial record HabboGroupDetailsMessageComposer(int GroupId, bool IsGuild, int Type, string GroupName, string Description, string BadgeCode, int RoomId, string RoomName, int Status, int TotalMembers, bool Favourite, string CreationDate, bool IsOwner, bool IsAdmin, string OwnerName, bool OpenDetails, bool MembersCanDecorate, int PendingMemberCount, bool HasBoard);
