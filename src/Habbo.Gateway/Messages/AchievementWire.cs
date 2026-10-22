using Habbo.Protocol.Messages.Outgoing.Inventory.Achievements;
using Habbo.Silo.Abstractions.Achievements;

namespace Habbo.Gateway.Messages;

/// <summary>Turns achievement progress into what the client reads. Both the login push and the
/// dialog's own request send the same shape, so the mapping lives in one place.</summary>
internal static class AchievementWire
{
    public static AchievementData From(AchievementProgressView progress) => AchievementData.From(progress);
}
