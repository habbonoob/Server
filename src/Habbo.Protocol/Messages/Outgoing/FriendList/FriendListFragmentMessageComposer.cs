using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>3816 FriendListFragmentMessageComposer — the friend list, in pieces. The client stitches
/// the fragments back together by their number, so a list that fits in one is still sent as one
/// fragment of one rather than as no fragments at all.</summary>
[Message(ServerMessages.FriendListFragmentMessageComposer)]
public sealed partial record FriendListFragmentMessageComposer(
    int TotalFragments,
    int FragmentNumber,
    IReadOnlyList<FriendData> Friends);
