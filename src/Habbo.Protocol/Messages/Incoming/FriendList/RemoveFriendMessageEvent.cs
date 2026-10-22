using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.FriendList;

/// <summary>2484 RemoveFriend — drop these friends. The client sends a list because the friend
/// list lets a player select several and remove them at once.</summary>
[Incoming(ClientMessages.RemoveFriendMessageEvent)]
public sealed partial record RemoveFriendMessageEvent(IReadOnlyList<int> FriendIds);
