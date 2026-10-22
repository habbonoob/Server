namespace Habbo.Domain.Polls;

/// <summary>A question of a poll: free text or a choice of one or several, possibly following up another.</summary>
public sealed class PollQuestion
{
    public const int FreeText = 0;
    public const int SingleChoice = 1;
    public const int MultipleChoice = 2;

    public int Id { get; set; }

    public int PollId { get; set; }

    /// <summary>The question it follows up; null for one of the poll's own.</summary>
    public int? ParentId { get; set; }

    public int SortOrder { get; set; }

    public int Type { get; set; }

    public string Text { get; set; } = string.Empty;

    public int Category { get; set; }

    public int AnswerType { get; set; }

    public List<PollChoice> Choices { get; set; } = [];
}
