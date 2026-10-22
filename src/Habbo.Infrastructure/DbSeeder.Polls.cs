using Habbo.Domain.Polls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Infrastructure;

/// <summary>The polls the hotel runs.</summary>
public static partial class DbSeeder
{
    private sealed record SeedPolls(List<SeedPoll>? Polls);

    private sealed record SeedPoll(
        int Id, string Type, string Headline, string Summary, string StartMessage, string EndMessage, int? RoomId, bool NpsPoll,
        List<SeedPollQuestion>? Questions);

    private sealed record SeedPollQuestion(
        int Id, int Type, string Text, int Category, int AnswerType, List<SeedPollChoice>? Choices, List<SeedPollQuestion>? FollowUps);

    private sealed record SeedPollChoice(string Value, string Text, int ChoiceType);

    /// <summary>seed/polls.json. A poll the hotel already has keeps whatever it was changed to.</summary>
    private static async Task SeedPollsAsync(AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        var file = await seedFiles.ReadJsonAsync<SeedPolls>("polls.json", ct);
        var known = await db.Polls.Select(p => p.Id).ToListAsync(ct);
        var added = 0;
        foreach (var seed in (file?.Polls ?? []).Where(p => !known.Contains(p.Id)))
        {
            var poll = new Poll
            {
                Id = seed.Id, Type = seed.Type, Headline = seed.Headline, Summary = seed.Summary, StartMessage = seed.StartMessage,
                EndMessage = seed.EndMessage, RoomId = seed.RoomId, NpsPoll = seed.NpsPoll,
            };
            var order = 0;
            foreach (var question in seed.Questions ?? [])
            {
                var asked = Question(seed.Id, null, question, order++);
                poll.Questions.Add(asked);
                var followOrder = 0;
                foreach (var followUp in question.FollowUps ?? [])
                    poll.Questions.Add(Question(seed.Id, question.Id, followUp, followOrder++));
            }

            db.Polls.Add(poll);
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} poll(s) from {File}.", added, "polls.json");
        }
    }

    private static PollQuestion Question(int pollId, int? parentId, SeedPollQuestion seed, int sortOrder)
    {
        var position = 0;
        return new PollQuestion
        {
            Id = seed.Id, PollId = pollId, ParentId = parentId, SortOrder = sortOrder, Type = seed.Type, Text = seed.Text,
            Category = seed.Category, AnswerType = seed.AnswerType,
            Choices = (seed.Choices ?? []).Select(c => new PollChoice
            {
                QuestionId = seed.Id, Position = position++, Value = c.Value, Text = c.Text, ChoiceType = c.ChoiceType,
            }).ToList(),
        };
    }
}
