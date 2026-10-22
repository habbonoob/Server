using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>ConsoleMessageHistoryComposer</summary>
[Message(ServerMessages.ConsoleMessageHistoryComposer)]
public sealed partial record ConsoleMessageHistoryComposer(int ChatId, IReadOnlyList<MessageHistoryEntryData> Entries);
