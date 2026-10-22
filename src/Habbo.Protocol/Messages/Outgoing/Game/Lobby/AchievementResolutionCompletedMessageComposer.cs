using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Lobby;

/// <summary>AchievementResolutionCompletedMessageComposer — a resolution's challenge done: the statue's class and the badge engraved on it</summary>
[Message(ServerMessages.AchievementResolutionCompletedMessageComposer)]
public sealed partial record AchievementResolutionCompletedMessageComposer(string StuffCode, string BadgeCode);
