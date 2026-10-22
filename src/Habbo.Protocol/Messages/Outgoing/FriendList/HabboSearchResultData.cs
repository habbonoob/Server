using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>HabboSearchResultData</summary>
[WireStruct]
public sealed partial record HabboSearchResultData(int AvatarId, string AvatarName, string AvatarMotto, bool IsAvatarOnline, bool CanFollow, string Unused, int AvatarGender, string AvatarFigure, string RealName);
