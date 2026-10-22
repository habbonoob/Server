namespace Habbo.Domain.Moderation;

/// <summary>A chat line the reporter picked as evidence.</summary>
public sealed class ModerationIssueChatLine
{
    public int Id { get; set; }

    public int IssueId { get; set; }

    /// <summary>Who said it, by account id as the client named them.</summary>
    public int UserAccountId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;
}
