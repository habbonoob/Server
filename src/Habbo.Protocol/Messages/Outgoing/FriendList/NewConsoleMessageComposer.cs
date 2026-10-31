using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>3511 NewConsoleMessageComposer — a message arriving in the console. It goes to the
/// reader; the sender's own client draws what it sent without waiting for this.</summary>
[Message(ServerMessages.NewConsoleMessageComposer)]
public sealed partial record NewConsoleMessageComposer(
    /// <summary>Whose conversation it belongs to — the other person in it.</summary>
    int ChatId,
    /// <summary>What a client older than September 2026 reads: always text, a Habbicon's name for a Habbicon.</summary>
    [property: WireUntil(ClientReleases.September2026)] string OlderClientText,
    /// <summary>September 2026 is told what kind of message it is first: text, or a Habbicon.</summary>
    [property: WireSince(ClientReleases.September2026)] ConsoleMessageType MessageType,
    [property: WireSince(ClientReleases.September2026), WireWhen("IsText")] string? MessageText,
    [property: WireSince(ClientReleases.September2026), WireWhen("IsHabbicon")] int? HabbiconId,
    int SecondsSinceSent,
    string MessageId,
    /// <summary>Echoed back from the sender's own counter so their client can match it up.</summary>
    int ConfirmationId,
    int SenderId,
    string SenderName,
    string SenderFigure)
{
    /// <summary>A text message (or, given its name as the text, a Habbicon: see <see cref="ConsoleMessageType"/>).</summary>
    public NewConsoleMessageComposer(int chatId, ConsoleMessageType messageType, string messageText, int secondsSinceSent,
        string messageId, int confirmationId, int senderId, string senderName, string senderFigure, int habbiconId = 0)
        : this(chatId, messageText, messageType, messageText, habbiconId, secondsSinceSent, messageId, confirmationId,
            senderId, senderName, senderFigure)
    {
    }

    public bool IsText => MessageType == ConsoleMessageType.Text;

    public bool IsHabbicon => MessageType == ConsoleMessageType.Habbicon;
}
