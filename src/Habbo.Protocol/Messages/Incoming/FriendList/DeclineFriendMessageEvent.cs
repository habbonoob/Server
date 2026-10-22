using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.FriendList;

/// <summary>69 DeclineFriend — turn down requests. The client sends "all" as a flag with an
/// empty list rather than listing every request, so a player clearing the lot sends one small
/// message however many are waiting.</summary>
[Incoming(ClientMessages.DeclineFriendMessageEvent)]
public sealed partial record DeclineFriendMessageEvent(
    bool DeclineAll,
    IReadOnlyList<int> RequesterIds);
