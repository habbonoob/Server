using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>GuildMembersMessageComposer</summary>
[Message(ServerMessages.GuildMembersMessageComposer)]
public sealed partial record GuildMembersMessageComposer(int GroupId, string GroupName, int BaseRoomId, string BadgeCode, int TotalEntries, IReadOnlyList<MemberData> Entries, bool AllowedToManage, int PageSize, int PageIndex, int SearchType, string UserNameFilter);
