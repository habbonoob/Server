using Habbo.Silo.Abstractions.Moderation;
using Habbo.Silo.Abstractions.Players;
using Orleans.Runtime;

namespace Habbo.Silo.Help;

/// <summary>A bully report the guardians are judging: who reported whom where, the chat they judge, and their seats.</summary>
public sealed class ChatReview
{
    public required PlayerData Reporter { get; init; }

    public required Guid ReportedId { get; init; }

    public required int ReportedAccountId { get; init; }

    public required int RoomId { get; init; }

    public required string Record { get; init; }

    public required IReadOnlyList<ReportedChatLine> Lines { get; init; }

    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;

    public List<ReviewSeat> Seats { get; } = [];

    /// <summary>The guardians who passed on it, or were offered it already.</summary>
    public HashSet<Guid> Asked { get; } = [];

    public IGrainTimer? VotingTimer { get; set; }
}
