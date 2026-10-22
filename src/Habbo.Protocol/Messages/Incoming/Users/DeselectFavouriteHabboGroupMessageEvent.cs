using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>DeselectFavouriteHabboGroupMessageEvent</summary>
[Incoming(ClientMessages.DeselectFavouriteHabboGroupMessageEvent)]
public sealed partial record DeselectFavouriteHabboGroupMessageEvent(int GroupId);
