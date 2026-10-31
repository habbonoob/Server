using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Quest;

/// <summary>SeasonalQuestsMessageComposer — the seasonal campaigns running, each with the quest the player is on in it.
/// The calendar (seasonalQuestCalendar.*) draws those whose campaign starts with its prefix, a day each.</summary>
[Message(ServerMessages.SeasonalQuestsMessageComposer)]
public sealed partial record SeasonalQuestsMessageComposer(IReadOnlyList<QuestMessageData> Quests);
