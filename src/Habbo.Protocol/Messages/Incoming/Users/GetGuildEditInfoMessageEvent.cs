using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>GetGuildEditInfoMessageEvent — a group as its manage window edits it</summary>
[Incoming(ClientMessages.GetGuildEditInfoMessageEvent)]
public sealed partial record GetGuildEditInfoMessageEvent(int GroupId);
