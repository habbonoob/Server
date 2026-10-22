using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Achievements;

/// <summary>954 AchievementsComposer — every achievement the hotel has, with this player's progress
/// through each. The client builds its whole achievement dialog from this one message and will
/// not open the window until it arrives, so an achievement left out of it does not exist as far
/// as the player is concerned. An entry with an empty category is dropped on arrival.</summary>
[Message(ServerMessages.AchievementsComposer)]
public sealed partial record AchievementsComposer(
    IReadOnlyList<AchievementData> Achievements,
    string DefaultCategory);
