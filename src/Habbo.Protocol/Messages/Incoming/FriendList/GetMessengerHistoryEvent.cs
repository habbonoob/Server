using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.FriendList;

/// <summary>GetMessengerHistoryEvent — older messages of a conversation, before this one (empty for the newest)</summary>
[Incoming(ClientMessages.GetMessengerHistoryEvent)]
public sealed partial record GetMessengerHistoryEvent(int ChatId, string MessageId);
