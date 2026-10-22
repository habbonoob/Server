using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Game.Lobby;

/// <summary>GetResolutionAchievementsMessageEvent — a resolution statue's challenges (achievement 0), or picking one</summary>
[Incoming(ClientMessages.GetResolutionAchievementsMessageEvent)]
public sealed partial record GetResolutionAchievementsMessageEvent(int StuffId, int AchievementId);
