namespace Habbo.Domain.Support;

/// <summary>A question of a quiz, by its number in the quiz's texts, and the number of its right answer.</summary>
public sealed class QuizQuestion
{
    public required string QuizCode { get; set; }

    public int QuestionId { get; set; }

    public int CorrectAnswer { get; set; }
}
