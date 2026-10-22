using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>PopularRoomsSearchMessageEvent</summary>
[Incoming(ClientMessages.PopularRoomsSearchMessageEvent)]
public sealed partial record PopularRoomsSearchMessageEvent(string Category, int AdIndex);
