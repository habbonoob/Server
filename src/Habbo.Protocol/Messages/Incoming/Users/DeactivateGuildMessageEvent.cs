using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>DeactivateGuildMessageEvent — the owner deletes the group</summary>
[Incoming(ClientMessages.DeactivateGuildMessageEvent)]
public sealed partial record DeactivateGuildMessageEvent(int GroupId);
