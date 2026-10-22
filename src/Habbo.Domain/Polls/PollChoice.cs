namespace Habbo.Domain.Polls;

/// <summary>A choice a question offers: the value stored when it is chosen, and the text shown.</summary>
public sealed class PollChoice
{
    public int QuestionId { get; set; }

    public int Position { get; set; }

    public string Value { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public int ChoiceType { get; set; }
}
