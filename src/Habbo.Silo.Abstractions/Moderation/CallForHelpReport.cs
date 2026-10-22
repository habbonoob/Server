namespace Habbo.Silo.Abstractions.Moderation;

/// <summary>What a player reports: a user, a room, instant messages, a forum post or a photo.</summary>
[GenerateSerializer]
public sealed partial record CallForHelpReport(
    [property: Id(0)] int Source,
    [property: Id(1)] int TopicId,
    [property: Id(2)] string Message,
    [property: Id(3)] int? ReportedAccountId,
    [property: Id(4)] int? RoomId,
    [property: Id(5)] IReadOnlyList<ReportedChatLine> ChatLines,
    [property: Id(6)] int? GroupId = null,
    [property: Id(7)] int? ThreadId = null,
    [property: Id(8)] int? ForumMessageId = null,
    [property: Id(9)] string PhotoId = "",
    [property: Id(10)] string ReporterName = "",
    [property: Id(11)] string ReporterEmail = "");
