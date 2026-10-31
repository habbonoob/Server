using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Quest;

/// <summary>QuestMessageComposer — the quest the player is on, taken on or moved on: the tracker shows it.</summary>
[Message(ServerMessages.QuestMessageComposer)]
public sealed partial record QuestMessageComposer(QuestMessageData Quest);
