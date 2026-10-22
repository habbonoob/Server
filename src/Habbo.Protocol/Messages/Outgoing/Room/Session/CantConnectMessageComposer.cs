using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Session;

/// <summary>CantConnectMessageComposer</summary>
[Message(ServerMessages.CantConnectMessageComposer)]
public sealed partial record CantConnectMessageComposer(int Reason, [property: WireWhen("CarriesParameter")] string? Parameter)
{
    // what the navigator explains, by reason
    public const int RoomFull = 1;
    public const int QueueError = 3;
    public const int Banned = 4;
    public const int Blocked = 5;

    /// <summary>Only a queue error names its reason, as a room.queue.error text key.</summary>
    public bool CarriesParameter => Reason == QueueError;
}
