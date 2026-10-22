using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>2539 NavigatorSettingsComposer — roomIdToEnter ≤ 0 makes the hotel view initialize.</summary>
[Message(ServerMessages.NavigatorSettingsComposer)]
public sealed partial record NavigatorSettingsComposer(int HomeRoomId, int RoomIdToEnter)
{
    /// <summary>roomIdToEnter value that keeps the client on the hotel view.</summary>
    public const int StayOnHotelView = 0;
}
