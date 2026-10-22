using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>2938 GetIgnoredUsers — the client asks who the player has muted.</summary>
[Incoming(ClientMessages.GetIgnoredUsersMessageEvent)]
public sealed partial record GetIgnoredUsersMessageEvent;
