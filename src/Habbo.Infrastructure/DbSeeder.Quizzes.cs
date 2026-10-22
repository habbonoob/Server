using Habbo.Domain.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Infrastructure;

/// <summary>The help quizzes and their right answers.</summary>
public static partial class DbSeeder
{
    private sealed record SeedQuizzes(List<SeedQuiz>? Quizzes);

    private sealed record SeedQuiz(string Code, string Achievement, List<SeedQuizQuestion>? Questions);

    private sealed record SeedQuizQuestion(int Id, int Correct);

    /// <summary>seed/quizzes.json. A quiz the hotel already has keeps whatever it was changed to.</summary>
    private static async Task SeedQuizzesAsync(AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        var file = await seedFiles.ReadJsonAsync<SeedQuizzes>("quizzes.json", ct);
        var known = await db.Quizzes.Select(q => q.Code).ToListAsync(ct);
        var added = 0;
        foreach (var quiz in (file?.Quizzes ?? []).Where(q => !known.Contains(q.Code)))
        {
            db.Quizzes.Add(new Quiz
            {
                Code = quiz.Code, AchievementName = quiz.Achievement,
                Questions = (quiz.Questions ?? []).Select(q => new QuizQuestion { QuizCode = quiz.Code, QuestionId = q.Id, CorrectAnswer = q.Correct }).ToList(),
            });
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} quiz(zes) from {File}.", added, "quizzes.json");
        }
    }
}
