using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Game.Lobby;

/// <summary>ResetResolutionAchievementMessageEvent — clearing a resolution statue so a new challenge can be picked</summary>
[Incoming(ClientMessages.ResetResolutionAchievementMessageEvent)]
public sealed partial record ResetResolutionAchievementMessageEvent(int StuffId);
