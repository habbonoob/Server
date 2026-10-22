using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>SelectFavouriteHabboGroupMessageEvent</summary>
[Incoming(ClientMessages.SelectFavouriteHabboGroupMessageEvent)]
public sealed partial record SelectFavouriteHabboGroupMessageEvent(int GroupId);
