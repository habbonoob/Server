using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.Trading;

/// <summary>OpenTradingEvent — start trading with an avatar in the room</summary>
[Incoming(ClientMessages.OpenTradingEvent)]
public sealed partial record OpenTradingEvent(int RoomIndex);
