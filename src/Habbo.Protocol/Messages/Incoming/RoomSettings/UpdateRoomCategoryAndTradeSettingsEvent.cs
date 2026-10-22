using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.RoomSettings;

/// <summary>UpdateRoomCategoryAndTradeSettingsEvent</summary>
[Incoming(ClientMessages.UpdateRoomCategoryAndTradeSettingsEvent)]
public sealed partial record UpdateRoomCategoryAndTradeSettingsEvent(int RoomId, int CategoryId, int TradeMode);
