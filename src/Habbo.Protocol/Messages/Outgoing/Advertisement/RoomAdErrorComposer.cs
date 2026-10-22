using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Advertisement;

/// <summary>RoomAdErrorComposer — a room event name (0) or description (1) with filtered words, shown as filtered</summary>
[Message(ServerMessages.RoomAdErrorComposer)]
public sealed partial record RoomAdErrorComposer(int ErrorCode, string FilteredText)
{
    public const int FilteredName = 0;
    public const int FilteredDescription = 1;
}
