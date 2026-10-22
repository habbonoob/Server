using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.Achievements;

/// <summary>2844 GetAchievements — the player opened the achievement dialog. The client asks
/// once and caches, so this arrives the first time the window is opened.</summary>
[Incoming(ClientMessages.GetAchievementsEvent)]
public sealed partial record GetAchievementsEvent;
