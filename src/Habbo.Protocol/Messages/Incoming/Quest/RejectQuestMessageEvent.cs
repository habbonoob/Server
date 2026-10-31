using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Quest;

/// <summary>RejectQuestMessageEvent — give up a quest that was taken on.</summary>
[Incoming(ClientMessages.RejectQuestMessageEvent)]
public sealed partial record RejectQuestMessageEvent(int QuestId);
