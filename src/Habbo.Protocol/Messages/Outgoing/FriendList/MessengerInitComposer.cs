using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>3946 MessengerInitComposer — how many friends the player may keep and the groups they
/// have made. The friends themselves follow in their own fragments.</summary>
[Message(ServerMessages.MessengerInitComposer)]
public sealed partial record MessengerInitComposer(
    int UserFriendLimit,
    int NormalFriendLimit,
    int ExtendedFriendLimit,
    IReadOnlyList<FriendCategoryData> Categories);
