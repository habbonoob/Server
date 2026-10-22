using Habbo.Domain.Moderation;

namespace Habbo.Silo.Abstractions.Moderation;

/// <summary>A sanction a moderator gives by hand.</summary>
[GenerateSerializer]
public sealed partial record ModerationAction(
    [property: Id(0)] SanctionKind Kind,
    [property: Id(1)] int AccountId,
    [property: Id(2)] string Message,
    [property: Id(3)] int TopicId,
    [property: Id(4)] int? IssueId,
    /// <summary>For a ban: the step of the ladder it is.</summary>
    [property: Id(5)] int? SanctionTypeId = null,
    /// <summary>For a trade lock: how long.</summary>
    [property: Id(6)] int? Minutes = null,
    [property: Id(7)] bool AvatarOnly = false);
