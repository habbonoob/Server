using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>HabboSearchResultComposer</summary>
[Message(ServerMessages.HabboSearchResultComposer)]
public sealed partial record HabboSearchResultComposer(IReadOnlyList<HabboSearchResultData> Friends, IReadOnlyList<HabboSearchResultData> Others);
