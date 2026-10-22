using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Lobby;

/// <summary>AchievementResolutionsMessageComposer — the achievements a resolution statue offers, and how long a challenge lasts in seconds</summary>
[Message(ServerMessages.AchievementResolutionsMessageComposer)]
public sealed partial record AchievementResolutionsMessageComposer(int StuffId, IReadOnlyList<AchievementResolutionData> Achievements, int EndTime);
