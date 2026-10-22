using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Achievements;

/// <summary>The help quizzes. Every call answers the player who made it.</summary>
public interface IQuizService
{
    /// <summary>The quiz's questions, in an order of their own for this player.</summary>
    Task SendQuestionsAsync(PlayerData player, string quizCode);

    /// <summary>Marks the answers, given in the order the questions were asked; all right passes the quiz.</summary>
    Task MarkAnswersAsync(PlayerData player, string quizCode, IReadOnlyList<int> answers);
}
