using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>MessageHistoryEntryData</summary>
[WireStruct]
public sealed partial record MessageHistoryEntryData(
    int SenderId,
    string SenderName,
    string SenderFigure,
    /// <summary>What a client older than September 2026 reads: always text, a Habbicon's name for a Habbicon.</summary>
    [property: WireUntil(ClientReleases.September2026)] string OlderClientMessage,
    /// <summary>September 2026 is told what kind of message it is first: text, or a Habbicon.</summary>
    [property: WireSince(ClientReleases.September2026)] ConsoleMessageType MessageType,
    [property: WireSince(ClientReleases.September2026), WireWhen("IsText")] string? Message,
    [property: WireSince(ClientReleases.September2026), WireWhen("IsHabbicon")] int? HabbiconId,
    int SecondsSinceSent,
    string MessageId)
{
    /// <summary>A text message (or, given its name as the text, a Habbicon: see <see cref="ConsoleMessageType"/>).</summary>
    public MessageHistoryEntryData(int senderId, string senderName, string senderFigure, ConsoleMessageType messageType,
        string message, int secondsSinceSent, string messageId, int habbiconId = 0)
        : this(senderId, senderName, senderFigure, message, messageType, message, habbiconId, secondsSinceSent, messageId)
    {
    }

    public bool IsText => MessageType == ConsoleMessageType.Text;

    public bool IsHabbicon => MessageType == ConsoleMessageType.Habbicon;
}
