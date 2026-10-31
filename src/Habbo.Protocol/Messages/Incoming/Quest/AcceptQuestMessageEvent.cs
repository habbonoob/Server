using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Quest;

/// <summary>AcceptQuestMessageEvent — take on a quest, from inside a room.</summary>
[Incoming(ClientMessages.AcceptQuestMessageEvent)]
public sealed partial record AcceptQuestMessageEvent(int QuestId);
