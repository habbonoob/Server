namespace Habbo.Silo.Help;

/// <summary>The guide tool and guardians (config section "Guides").</summary>
public sealed class GuideOptions
{
    /// <summary>The wait a requester is told to expect, and how long a guide has to take a request offered.</summary>
    public int EstimatedWaitSeconds { get; set; }
    public int OfferTimeoutSeconds { get; set; }

    /// <summary>How many guardians judge a bully report, and how many at least it needs.</summary>
    public int GuardiansPerReview { get; set; }
    public int MinimumGuardians { get; set; }

    /// <summary>How long a guardian has to take a review offered, and to vote.</summary>
    public int ReviewOfferTimeoutSeconds { get; set; }
    public int VotingTimeoutSeconds { get; set; }

    /// <summary>How far back, and how many lines of, the room's chat a review shows.</summary>
    public int ReviewChatMinutes { get; set; }
    public int ReviewChatLines { get; set; }

    /// <summary>How long a reporter waits between bully reports, and how many of theirs may be found
    /// acceptable within how many days before they may report no more.</summary>
    public int ReportCooldownSeconds { get; set; }
    public int InvalidReportsBeforeBlocked { get; set; }
    public int InvalidReportsDays { get; set; }

    /// <summary>The help topic a reported guide and a forwarded bully report are filed under.</summary>
    public string ReportTopicName { get; set; } = string.Empty;

    /// <summary>The achievements the guide tool moves on.</summary>
    public string RequesterAchievement { get; set; } = string.Empty;
    public string RequestHandlerAchievement { get; set; } = string.Empty;
    public string TourGiverAchievement { get; set; } = string.Empty;
    public string TourTakerAchievement { get; set; } = string.Empty;
    public string RecommendationAchievement { get; set; } = string.Empty;
    public string FeedbackGiverAchievement { get; set; } = string.Empty;
    public string ChatReviewRequesterAchievement { get; set; } = string.Empty;
    public string ChatReviewerAchievement { get; set; } = string.Empty;
    public string OnDutyAchievement { get; set; } = string.Empty;
}
