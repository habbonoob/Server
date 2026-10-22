using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>MessageHistoryEntryData</summary>
[WireStruct]
public sealed partial record MessageHistoryEntryData(
    int SenderId,
    string SenderName,
    string SenderFigure,
    /// <summary>September 2026 is told what kind of message it is first: text, or a Habbicon.</summary>
    [property: WireSince(ClientReleases.September2026)] ConsoleMessageType MessageType,
    string Message,
    int SecondsSinceSent,
    string MessageId);
