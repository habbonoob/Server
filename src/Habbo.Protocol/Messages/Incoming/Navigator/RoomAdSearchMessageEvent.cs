using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>RoomAdSearchMessageEvent</summary>
[Incoming(ClientMessages.RoomAdSearchMessageEvent)]
public sealed partial record RoomAdSearchMessageEvent(int AdIndex, int SearchType);
