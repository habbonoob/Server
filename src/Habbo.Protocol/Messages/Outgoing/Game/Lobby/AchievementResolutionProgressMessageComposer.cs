using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Lobby;

/// <summary>AchievementResolutionProgressMessageComposer — how far the statue's owner is with its challenge, and the seconds left</summary>
[Message(ServerMessages.AchievementResolutionProgressMessageComposer)]
public sealed partial record AchievementResolutionProgressMessageComposer(int StuffId, int AchievementId, string RequiredLevelBadgeCode, int UserProgress, int TotalProgress, int EndTime);
