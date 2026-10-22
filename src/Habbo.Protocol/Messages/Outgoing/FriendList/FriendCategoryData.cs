using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>One of the player's own friend groups.</summary>
[WireStruct]
public sealed partial record FriendCategoryData(int Id, string Name);
