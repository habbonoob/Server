using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>3277 ScrSendKickbackInfoMessageComposer — the club payday figures.</summary>
[Message(ServerMessages.ScrSendKickbackInfoMessageComposer)]
public sealed partial record ScrSendKickbackInfoMessageComposer(
    int CurrentHcStreak,
    string FirstSubscriptionDate,
    double KickbackPercentage,
    int TotalCreditsMissed,
    int TotalCreditsRewarded,
    int TotalCreditsSpent,
    int CreditRewardForStreakBonus,
    int CreditRewardForMonthlySpent,
    int TimeUntilPayday);
