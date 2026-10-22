namespace Habbo.Domain.Support;

/// <summary>A help quiz, by the code its texts go by (quiz.CODE.*): its questions, and the achievement
/// passing it moves on.</summary>
public sealed class Quiz
{
    public const int CodeMaxLength = 64;

    public required string Code { get; set; }

    public string AchievementName { get; set; } = string.Empty;

    public List<QuizQuestion> Questions { get; set; } = [];
}
