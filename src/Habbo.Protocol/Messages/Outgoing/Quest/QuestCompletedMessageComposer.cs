using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Quest;

/// <summary>QuestCompletedMessageComposer — a quest is done; with the dialog, the client shows its reward.</summary>
[Message(ServerMessages.QuestCompletedMessageComposer)]
public sealed partial record QuestCompletedMessageComposer(QuestMessageData Quest, bool ShowDialog);
