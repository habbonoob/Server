using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>2047 FriendListUpdateComposer — what changed in the friend list since the client last
/// heard. Each entry opens with a tag saying which of the three it is, so removals, additions
/// and changes travel together in one message.</summary>
[Message(ServerMessages.FriendListUpdateComposer)]
public sealed partial record FriendListUpdateComposer(
    IReadOnlyList<FriendCategoryData> Categories,
    IReadOnlyList<FriendListChangeData> Changes);
