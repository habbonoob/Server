using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Lobby;

/// <summary>AchievementResolutionData — one achievement a resolution can challenge, and whether it can</summary>
[WireStruct]
public sealed partial record AchievementResolutionData(int AchievementId, int Level, string BadgeId, int RequiredLevel, int State);
