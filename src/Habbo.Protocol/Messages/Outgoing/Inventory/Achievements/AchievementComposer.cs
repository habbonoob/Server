using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Achievements;

/// <summary>3188 AchievementComposer — where a player stands on one achievement. Sent for each of
/// them they have started at login, and again whenever one moves on.</summary>
[Message(ServerMessages.AchievementComposer)]
public sealed partial record AchievementComposer(AchievementData Achievement);
