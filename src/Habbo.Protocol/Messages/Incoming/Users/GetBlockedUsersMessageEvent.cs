using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>1613 BlockListInit — the client asks who the player has blocked.</summary>
[Incoming(ClientMessages.GetBlockedUsersMessageEvent)]
public sealed partial record GetBlockedUsersMessageEvent;
