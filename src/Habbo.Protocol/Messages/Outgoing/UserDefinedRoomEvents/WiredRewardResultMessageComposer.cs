using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>2267 WiredRewardResultMessageComposer — what came of a give reward box: a reward, a badge, or why there was
/// none (wiredfurni.rewardfailed.reason.N).</summary>
[Message(ServerMessages.WiredRewardResultMessageComposer)]
public sealed partial record WiredRewardResultMessageComposer(int Reason)
{
    public const int LimitReached = 0;
    public const int AlreadyGiven = 1;
    public const int GivenToday = 2;
    public const int GivenThisHour = 3;
    public const int Unlucky = 4;
    public const int AllCollected = 5;
    public const int ProductReceived = 6;
    public const int BadgeReceived = 7;
    public const int GivenThisMinute = 8;
}
