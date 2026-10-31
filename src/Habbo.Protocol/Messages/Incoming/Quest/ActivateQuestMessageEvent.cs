using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Quest;

/// <summary>ActivateQuestMessageEvent — take on a quest from the hotel view; as accepting it.</summary>
[Incoming(ClientMessages.ActivateQuestMessageEvent)]
public sealed partial record ActivateQuestMessageEvent(int QuestId);
