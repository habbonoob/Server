using Habbo.Silo.Abstractions.Moderation;
using Habbo.Silo.Abstractions.Players;
using Orleans.Runtime;

namespace Habbo.Silo.Help;

/// <summary>A request for a guide: waiting, offered to a guide, or taken and under way; with what was said.</summary>
public sealed class GuideRequest
{
    public required PlayerData Requester { get; init; }

    public required int Type { get; init; }

    public required string Description { get; init; }

    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;

    /// <summary>The guide it is offered to, waiting for their answer.</summary>
    public PlayerData? Offered { get; set; }
    public IGrainTimer? OfferTimer { get; set; }

    /// <summary>The guide who took it, and when.</summary>
    public PlayerData? Guide { get; set; }
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>The guides who passed on it.</summary>
    public HashSet<Guid> Passed { get; } = [];

    /// <summary>What was said, by account.</summary>
    public List<ReportedChatLine> Transcript { get; } = [];
}
