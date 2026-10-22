using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Notifications;

/// <summary>HabboAchievementNotificationMessageComposer — the player reached a level of an achievement</summary>
[Message(ServerMessages.HabboAchievementNotificationMessageComposer)]
public sealed partial record HabboAchievementNotificationMessageComposer(AchievementLevelUpData Data);
