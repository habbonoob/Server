using System.Collections.Concurrent;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Help;
using Habbo.Silo.Abstractions.Achievements;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;
using Orleans;

namespace Habbo.Silo.Help;

/// <summary>The help quizzes: the questions asked in a shuffled order, which is remembered so the
/// answers can be matched to them; passing moves the quiz's achievement on.</summary>
public sealed class QuizService(IDbContextFactory<AppDbContext> dbFactory, IGrainFactory grains, PlayerMessenger messenger) : IQuizService
{
    /// <summary>What passing a quiz is worth towards its achievement.</summary>
    private const int PassProgress = 1;

    /// <summary>The order each player was last asked each quiz's questions in.</summary>
    private readonly ConcurrentDictionary<(Guid Player, string Quiz), IReadOnlyList<int>> _asked = new();

    public async Task SendQuestionsAsync(PlayerData player, string quizCode)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var questions = await db.QuizQuestions.AsNoTracking().Where(q => q.QuizCode == quizCode).Select(q => q.QuestionId).ToListAsync();
        var order = questions.OrderBy(_ => Random.Shared.Next()).ToList();
        _asked[(player.Id, quizCode)] = order;
        await messenger.SendAsync(player.Id, new QuizDataMessageComposer(quizCode, order));
    }

    public async Task MarkAnswersAsync(PlayerData player, string quizCode, IReadOnlyList<int> answers)
    {
        if (!_asked.TryRemove((player.Id, quizCode), out var asked))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var quiz = await db.Quizzes.AsNoTracking().Include(q => q.Questions).FirstOrDefaultAsync(q => q.Code == quizCode);
        if (quiz is null)
            return;

        var correct = quiz.Questions.ToDictionary(q => q.QuestionId, q => q.CorrectAnswer);
        var wrong = asked.Where((question, i) => i >= answers.Count || !correct.TryGetValue(question, out var right) || answers[i] != right).ToList();
        await messenger.SendAsync(player.Id, new QuizResultsMessageComposer(quizCode, wrong));

        if (wrong.Count == 0 && quiz.AchievementName.Length > 0)
            await grains.GetGrain<IPlayerGrain>(player.Id).ProgressAchievementAsync(quiz.AchievementName, PassProgress);
    }
}
