using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.FriendList;

/// <summary>SetRelationshipStatusMessageEvent — mark a friend (heart, smile, bobba or none)</summary>
[Incoming(ClientMessages.SetRelationshipStatusMessageEvent)]
public sealed partial record SetRelationshipStatusMessageEvent(int FriendId, int Status);
