using Orleans;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Moderation;

/// <summary>The hotel's guide desk (key 0): who is on duty, requests for guides and their sessions, and
/// bully reports judged by guardians.</summary>
public interface IGuideDeskGrain : IGrainWithIntegerKey
{
    Task UpdateDutyAsync(PlayerData player, bool onDuty, bool tours, bool helps, bool chatReviews);

    Task CreateRequestAsync(PlayerData player, int type, string description);

    Task GuideDecidesAsync(PlayerData player, bool accept);

    Task RequesterCancelsAsync(PlayerData player);

    Task ResolveAsync(PlayerData player);

    Task SayAsync(PlayerData player, string message);

    Task TypingAsync(PlayerData player, bool typing);

    Task InviteRequesterAsync(PlayerData player);

    Task GoToRequesterRoomAsync(PlayerData player);

    Task FeedbackAsync(PlayerData player, bool recommend);

    Task ReportGuideAsync(PlayerData player, string message);

    Task SendReportingStatusAsync(PlayerData player);

    Task CreateChatReviewAsync(PlayerData player, int reportedAccountId, int roomId);

    Task ReviewDecidesAsync(PlayerData player, bool accept);

    Task VoteAsync(PlayerData player, int vote);

    Task LeaveReviewAsync(PlayerData player);

    /// <summary>The player went offline: off duty, and out of any request or review.</summary>
    Task PlayerLeftAsync(Guid playerId);
}
