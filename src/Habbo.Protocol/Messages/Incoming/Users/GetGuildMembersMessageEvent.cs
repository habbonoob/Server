using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>GetGuildMembersMessageEvent — a page of a group's member list</summary>
[Incoming(ClientMessages.GetGuildMembersMessageEvent)]
public sealed partial record GetGuildMembersMessageEvent(int GroupId, int PageIndex, string UserNameFilter, int SearchType);
