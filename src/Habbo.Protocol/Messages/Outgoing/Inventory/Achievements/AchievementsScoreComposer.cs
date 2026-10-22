using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Achievements;

/// <summary>2863 AchievementsScoreComposer — the total the client shows on the player's own badge
/// and profile. It is the sum of the points every unlocked achievement level is worth.</summary>
[Message(ServerMessages.AchievementsScoreComposer)]
public sealed partial record AchievementsScoreComposer(int Score);
