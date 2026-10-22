using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>3511 NewConsoleMessageComposer — a message arriving in the console. It goes to the
/// reader; the sender's own client draws what it sent without waiting for this.</summary>
[Message(ServerMessages.NewConsoleMessageComposer)]
public sealed partial record NewConsoleMessageComposer(
    /// <summary>Whose conversation it belongs to — the other person in it.</summary>
    int ChatId,
    /// <summary>September 2026 is told what kind of message it is first: text, or a Habbicon.</summary>
    [property: WireSince(ClientReleases.September2026)] ConsoleMessageType MessageType,
    string MessageText,
    int SecondsSinceSent,
    string MessageId,
    /// <summary>Echoed back from the sender's own counter so their client can match it up.</summary>
    int ConfirmationId,
    int SenderId,
    string SenderName,
    string SenderFigure);
