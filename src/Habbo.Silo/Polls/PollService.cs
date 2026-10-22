using Habbo.Domain.Polls;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Poll;
using Habbo.Silo.Abstractions.Navigator;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Polls;

/// <summary>The hotel's polls (seed/polls.json): offered once to each player entering a room that runs one,
/// taken or turned down, and answered question by question. What each player did is kept.</summary>
public sealed class PollService(IDbContextFactory<AppDbContext> dbFactory, PlayerMessenger messenger) : IPollService
{
    /// <summary>The most answers one question takes, and how long each may be.</summary>
    private const int MaxAnswers = 50;
    private const int MaxAnswerLength = 255;

    public async Task OfferAsync(PlayerData player, int roomId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var poll = await db.Polls.AsNoTracking()
            .Where(p => p.Active && (p.RoomId == roomId || p.RoomId == null) && !db.PollResponses.Any(r => r.PollId == p.Id && r.UserId == player.Id))
            .OrderByDescending(p => p.RoomId).FirstOrDefaultAsync();
        if (poll is not null)
            await messenger.SendAsync(player.Id, new PollOfferComposer(poll.Id, poll.Type, poll.Headline, poll.Summary));
    }

    public async Task StartAsync(PlayerData player, int pollId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var poll = await db.Polls.AsNoTracking().Include(p => p.Questions).ThenInclude(q => q.Choices)
            .FirstOrDefaultAsync(p => p.Id == pollId && p.Active);
        if (poll is null || await db.PollResponses.AnyAsync(r => r.PollId == pollId && r.UserId == player.Id && r.QuestionId == null))
        {
            await messenger.SendAsync(player.Id, new PollErrorComposer());
            return;
        }

        var questions = poll.Questions.Where(q => q.ParentId is null).OrderBy(q => q.SortOrder)
            .Select(q => new PollQuestionWithFollowUpsData(PollQuestionData.From(q),
                poll.Questions.Where(c => c.ParentId == q.Id).OrderBy(c => c.SortOrder).Select(PollQuestionData.From).ToList()))
            .ToList();
        await messenger.SendAsync(player.Id, new PollContentsComposer(poll.Id, poll.StartMessage, poll.EndMessage, questions, poll.NpsPoll));
    }

    /// <summary>Turning a poll down is kept, so it is not offered again.</summary>
    public async Task RejectAsync(PlayerData player, int pollId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await db.Polls.AnyAsync(p => p.Id == pollId) || await db.PollResponses.AnyAsync(r => r.PollId == pollId && r.UserId == player.Id))
            return;

        db.PollResponses.Add(new PollResponse { PollId = pollId, UserId = player.Id });
        await db.SaveChangesAsync();
    }

    /// <summary>An answer to a question of the poll, once each; a choice question takes only its own choices.</summary>
    public async Task AnswerAsync(PlayerData player, int pollId, int questionId, IReadOnlyList<string> answers)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var question = await db.PollQuestions.AsNoTracking().Include(q => q.Choices).FirstOrDefaultAsync(q => q.Id == questionId && q.PollId == pollId);
        if (question is null || answers.Count == 0 || answers.Count > MaxAnswers
            || await db.PollResponses.AnyAsync(r => r.PollId == pollId && r.UserId == player.Id && (r.QuestionId == questionId || r.QuestionId == null)))
            return;

        var given = answers.Select(a => a.Length > MaxAnswerLength ? a[..MaxAnswerLength] : a).ToList();
        if (question.Type is PollQuestion.SingleChoice or PollQuestion.MultipleChoice)
        {
            var values = question.Choices.Select(c => c.Value).ToHashSet();
            if (given.Any(a => !values.Contains(a)) || (question.Type == PollQuestion.SingleChoice && given.Count != 1))
                return;
        }

        db.PollResponses.Add(new PollResponse { PollId = pollId, UserId = player.Id, QuestionId = questionId, Answers = given });
        await db.SaveChangesAsync();
    }
}
