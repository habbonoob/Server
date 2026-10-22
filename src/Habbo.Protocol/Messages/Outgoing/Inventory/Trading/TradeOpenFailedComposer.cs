using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Trading;

/// <summary>TradeOpenFailedComposer — inventory.trading.openfail.N</summary>
[Message(ServerMessages.TradeOpenFailedComposer)]
public sealed partial record TradeOpenFailedComposer(int Reason, string OtherUserName)
{
    public const int HotelDisabled = 1;
    public const int YouAreTradeLocked = 3;
    public const int OtherCannotTrade = 4;
    public const int RoomDisallows = 6;
    public const int YouAreTrading = 7;
    public const int OtherIsTrading = 8;
}
