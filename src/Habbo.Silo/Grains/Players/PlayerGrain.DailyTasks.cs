using Habbo.Domain.DailyTasks;
using Habbo.Domain.Users;
using Habbo.Protocol.Messages.Outgoing.Quest.DailyTasks;
using Habbo.Silo.RewardTracks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Habbo.Silo.Grains.Players;

/// <summary>Daily tasks: three a day from the pool, new at midnight UTC, each done by doing what it counts so many
/// times, its reward claimed by hand. A done task not claimed stays to be claimed on later days; one not done ends
/// with its day.</summary>
public sealed partial class PlayerGrain
{
    /// <summary>The currency type ducket rewards are in, which club members get twice ("You get double duckets as you
    /// are an HC member!").</summary>
    private const string DucketCurrency = "0";

    private DailyTaskPool DailyTaskPool => ServiceProvider.GetRequiredService<DailyTaskPool>();

    public async Task SendDailyTasksAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var pool = await DailyTaskPool.AllAsync();
        await using var db = await dbFactory.CreateDbContextAsync();
        var today = await TodaysTasksAsync(db, pool, now);
        var unclaimed = await db.DailyTasks.AsNoTracking()
            .Where(t => t.UserId == PlayerId && t.Day < DailyTaskRules.Today(now) && t.Status == DailyTask.Completed)
            .OrderBy(t => t.Id).ToListAsync();

        Send(new DailyTasksActiveListMessageComposer(today.Concat(unclaimed)
            .Select(task => ToWire(task, pool, now)).OfType<DailyTaskData>().ToList()));
    }

    /// <summary>Counts an action on today's tasks that count it.</summary>
    private async Task CountDailyTasksAsync(string action, string parameter, int amount, int effectId)
    {
        var pool = await DailyTaskPool.AllAsync();
        if (amount <= 0 || !pool.Any(t => t.Counts(action, parameter, effectId)))
            return;

        var now = DateTimeOffset.UtcNow;
        var day = DailyTaskRules.Today(now);
        await using var db = await dbFactory.CreateDbContextAsync();
        var tasks = await db.DailyTasks.Where(t => t.UserId == PlayerId && t.Day == day && t.Status == DailyTask.InProgress).ToListAsync();
        var changed = new List<DailyTask>();
        foreach (var task in tasks)
        {
            if (pool.FirstOrDefault(t => t.Code == task.Code) is not { } definition || !definition.Counts(action, parameter, effectId))
                continue;

            task.Repeats = Math.Min(task.Repeats + amount, definition.Repeats);
            if (task.Repeats >= definition.Repeats)
                task.Status = DailyTask.Completed;
            changed.Add(task);
        }

        if (changed.Count == 0)
            return;
        await db.SaveChangesAsync();
        foreach (var task in changed)
            Send(new DailyTasksTaskUpdateMessageComposer(task.Id, task.Repeats, task.Status, DailyTaskRules.SecondsLeft(now)));
    }

    public async Task ClaimDailyTaskAsync(int taskId)
    {
        var now = DateTimeOffset.UtcNow;
        var pool = await DailyTaskPool.AllAsync();
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.DailyTasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == PlayerId && t.Status == DailyTask.Completed) is not { } task
            || pool.FirstOrDefault(t => t.Code == task.Code) is not { } definition)
            return;

        var clubMember = (Data?.ClubLevel ?? ClubLevel.None) != ClubLevel.None;
        await using var transaction = await db.Database.BeginTransactionAsync();
        var told = new List<Func<Task>>();
        foreach (var reward in definition.Rewards)
        {
            var doubled = reward.Type == RewardTrackPrizeTypes.Currency && reward.Item == DucketCurrency && clubMember
                ? reward with { Amount = reward.Amount * 2 }
                : reward;
            if (await GrantPrizeAsync(db, doubled, CurrencyTransactionReasons.DailyTaskReward) is { } tell)
                told.Add(tell);
        }

        task.Status = DailyTask.Claimed;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        foreach (var tell in told)
            await tell();
        Send(new DailyTasksTaskUpdateMessageComposer(task.Id, task.Repeats, task.Status,
            task.Day == DailyTaskRules.Today(now) ? DailyTaskRules.SecondsLeft(now) : -1));
    }

    /// <summary>Today's tasks, drawn from the pool the first time the player asks for them.</summary>
    private async Task<List<DailyTask>> TodaysTasksAsync(Habbo.Infrastructure.AppDbContext db, IReadOnlyList<DailyTaskDefinition> pool, DateTimeOffset now)
    {
        var day = DailyTaskRules.Today(now);
        var tasks = await db.DailyTasks.Where(t => t.UserId == PlayerId && t.Day == day).OrderBy(t => t.Id).ToListAsync();
        if (tasks.Count > 0 || pool.Count == 0)
            return tasks;

        tasks = DailyTaskRules.Draw(pool, PlayerId, day).Select(d => new DailyTask { UserId = PlayerId, Day = day, Code = d.Code }).ToList();
        db.DailyTasks.AddRange(tasks);
        await db.SaveChangesAsync();
        return tasks;
    }

    /// <summary>The task as the client lists it; null for one the pool no longer has.</summary>
    private static DailyTaskData? ToWire(DailyTask task, IReadOnlyList<DailyTaskDefinition> pool, DateTimeOffset now) =>
        pool.FirstOrDefault(t => t.Code == task.Code) is not { } definition
            ? null
            : new DailyTaskData(task.Id, task.Code, definition.Action, IsBonus: false, ImageVersion: string.Empty, CatalogName: string.Empty,
                definition.Repeats, task.Repeats, task.Status,
                task.Day == DailyTaskRules.Today(now) ? DailyTaskRules.SecondsLeft(now) : -1,
                definition.Rewards.Select(r => new DailyTaskRewardData(r.Type, r.Item, r.Extra, r.Amount)).ToList());
}
