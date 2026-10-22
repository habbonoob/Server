using Habbo.Domain.Furniture;
using Habbo.Domain.Furniture.Data;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Furni;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Items;

/// <summary>Reads and writes an item's data across the furni_* tables.
///
/// The item records only which layout it uses; the payload lives in the table for that layout.
/// Keeping the reads here rather than as navigations on UserItem stops every item query from
/// dragging nine joins along, and lets a load touch only the tables its items actually need.</summary>
public static class FurniDataStore
{
    /// <summary>The data for each item, keyed by item id. Tables are queried only for layouts
    /// present among the items, so a room of ordinary furniture costs one query.</summary>
    public static async Task<Dictionary<int, FurniDataView>> LoadAsync(
        AppDbContext db, IReadOnlyCollection<UserItem> items, CancellationToken ct = default)
    {
        var views = new Dictionary<int, FurniDataView>(items.Count);
        if (items.Count == 0)
            return views;

        var ids = items.Select(item => item.Id).ToList();
        bool Needs(params FurniDataType[] types) => items.Any(item => types.Contains(item.DataType));

        var states = Needs(FurniDataType.Legacy, FurniDataType.VoteResult, FurniDataType.HighScore, FurniDataType.Crackable)
            ? await db.FurniStates.AsNoTracking().Where(s => ids.Contains(s.ItemId))
                .ToDictionaryAsync(s => s.ItemId, s => s.State, ct)
            : [];

        var maps = Needs(FurniDataType.Map)
            ? (await db.FurniMapEntries.AsNoTracking().Where(e => ids.Contains(e.ItemId)).ToListAsync(ct))
                .ToLookup(e => e.ItemId)
            : Enumerable.Empty<FurniMapEntry>().ToLookup(e => e.ItemId);

        var strings = Needs(FurniDataType.StringArray)
            ? (await db.FurniStringEntries.AsNoTracking().Where(e => ids.Contains(e.ItemId)).ToListAsync(ct))
                .ToLookup(e => e.ItemId)
            : Enumerable.Empty<FurniStringEntry>().ToLookup(e => e.ItemId);

        var ints = Needs(FurniDataType.IntArray)
            ? (await db.FurniIntEntries.AsNoTracking().Where(e => ids.Contains(e.ItemId)).ToListAsync(ct))
                .ToLookup(e => e.ItemId)
            : Enumerable.Empty<FurniIntEntry>().ToLookup(e => e.ItemId);

        var votes = Needs(FurniDataType.VoteResult)
            ? await db.FurniVoteResults.AsNoTracking().Where(v => ids.Contains(v.ItemId))
                .ToDictionaryAsync(v => v.ItemId, v => v.Result, ct)
            : [];

        var crackables = Needs(FurniDataType.Crackable)
            ? await db.FurniCrackables.AsNoTracking().Where(c => ids.Contains(c.ItemId))
                .ToDictionaryAsync(c => c.ItemId, ct)
            : [];

        var boards = Needs(FurniDataType.HighScore)
            ? await db.FurniHighScores.AsNoTracking().Where(h => ids.Contains(h.ItemId))
                .Include(h => h.Entries).ThenInclude(e => e.Users)
                .ToDictionaryAsync(h => h.ItemId, ct)
            : [];

        foreach (var item in items)
        {
            // A layout whose row is missing still has to write every field it declares, or the
            // packet desyncs; defaults stand in rather than nulls.
            var state = states.GetValueOrDefault(item.Id, "");
            var view = FurniDataView.Plain with
            {
                Type = (int)item.DataType,
                State = null,
                UniqueSerialNumber = item.UniqueSerialNumber,
                UniqueSeriesSize = item.UniqueSeriesSize,
            };

            var crackable = crackables.GetValueOrDefault(item.Id);

            views[item.Id] = item.DataType switch
            {
                FurniDataType.Legacy => view with { State = state },
                FurniDataType.Map => view with
                {
                    MapEntries = maps[item.Id].OrderBy(e => e.Key, StringComparer.Ordinal)
                        .Select(e => new FurniMapEntryView(e.Key, e.Value)).ToList(),
                },
                FurniDataType.StringArray => view with
                {
                    StringValues = strings[item.Id].OrderBy(e => e.Position).Select(e => e.Value).ToList(),
                },
                FurniDataType.VoteResult => view with
                {
                    State = state,
                    VoteResult = votes.GetValueOrDefault(item.Id),
                },
                FurniDataType.IntArray => view with
                {
                    IntValues = ints[item.Id].OrderBy(e => e.Position).Select(e => e.Value).ToList(),
                },
                FurniDataType.HighScore => HighScore(view, state, boards.GetValueOrDefault(item.Id)),
                FurniDataType.Crackable => view with
                {
                    State = state,
                    Hits = crackable?.Hits ?? 0,
                    Target = crackable?.Target ?? 0,
                },
                _ => view,
            };
        }

        // group furniture shows its group as the group is now
        await GroupFurniData.FillAsync(db, items.Where(i => i.GroupId is not null).ToDictionary(i => i.Id, i => i.GroupId!.Value), views, ct);
        return views;
    }

    /// <summary>The data for one item.</summary>
    public static async Task<FurniDataView> LoadAsync(AppDbContext db, UserItem item, CancellationToken ct = default) =>
        (await LoadAsync(db, [item], ct))[item.Id];

    /// <summary>Records data for a new item. The rows reference the item through its
    /// navigation, so they are written in the same SaveChanges and pick up its id then.</summary>
    public static void Attach(AppDbContext db, UserItem item, FurniDataView data)
    {
        item.DataType = (FurniDataType)data.Type;
        item.UniqueSerialNumber = data.UniqueSerialNumber;
        item.UniqueSeriesSize = data.UniqueSeriesSize;
        AddRows(db, data, row => row.Item = item);
    }

    /// <summary>Replaces a stored item's data — a lamp switched, a dice rolled. Only the rows of
    /// the layout the item was in are cleared, so an ordinary state change costs one delete and
    /// one insert. The limited-edition numbers are fixed at purchase and are left alone.</summary>
    public static async Task SaveAsync(
        AppDbContext db, int itemId, FurniDataView previous, FurniDataView next, CancellationToken ct = default)
    {
        // the old rows go and the new come in one go, so nothing can see (or leave) an item with none or both;
        // inside the caller's transaction when it has one
        await using var transaction = db.Database.CurrentTransaction is null ? await db.Database.BeginTransactionAsync(ct) : null;
        await ClearAsync(db, itemId, (FurniDataType)previous.Type, ct);

        if (previous.Type != next.Type)
        {
            await db.UserItems.Where(i => i.Id == itemId)
                .ExecuteUpdateAsync(set => set.SetProperty(i => i.DataType, (FurniDataType)next.Type), ct);
        }

        AddRows(db, next, row => row.ItemId = itemId);
        await db.SaveChangesAsync(ct);
        if (transaction is not null)
            await transaction.CommitAsync(ct);
    }

    private static async Task ClearAsync(AppDbContext db, int itemId, FurniDataType type, CancellationToken ct)
    {
        switch (type)
        {
            case FurniDataType.Map:
                await db.FurniMapEntries.Where(e => e.ItemId == itemId).ExecuteDeleteAsync(ct);
                break;
            case FurniDataType.StringArray:
                await db.FurniStringEntries.Where(e => e.ItemId == itemId).ExecuteDeleteAsync(ct);
                break;
            case FurniDataType.IntArray:
                await db.FurniIntEntries.Where(e => e.ItemId == itemId).ExecuteDeleteAsync(ct);
                break;
            case FurniDataType.VoteResult:
                await db.FurniVoteResults.Where(v => v.ItemId == itemId).ExecuteDeleteAsync(ct);
                break;
            case FurniDataType.Crackable:
                await db.FurniCrackables.Where(c => c.ItemId == itemId).ExecuteDeleteAsync(ct);
                break;
            case FurniDataType.HighScore:
                await db.FurniHighScores.Where(h => h.ItemId == itemId).ExecuteDeleteAsync(ct);
                break;
        }

        // The leading state is shared by several layouts, so it is cleared whatever the layout.
        await db.FurniStates.Where(s => s.ItemId == itemId).ExecuteDeleteAsync(ct);
    }

    /// <summary>Adds the rows for <paramref name="data"/>; <paramref name="link"/> ties each one
    /// to its item, by navigation for a new item or by id for a stored one.</summary>
    private static void AddRows(AppDbContext db, FurniDataView data, Action<IFurniRow> link)
    {
        T Linked<T>(T row) where T : IFurniRow
        {
            link(row);
            return row;
        }

        if (data.State is { Length: > 0 } state)
            db.FurniStates.Add(Linked(new FurniState { State = state }));

        foreach (var entry in data.MapEntries ?? [])
            db.FurniMapEntries.Add(Linked(new FurniMapEntry { Key = entry.Key, Value = entry.Value }));

        var position = 0;
        foreach (var value in data.StringValues ?? [])
            db.FurniStringEntries.Add(Linked(new FurniStringEntry { Position = position++, Value = value }));

        position = 0;
        foreach (var value in data.IntValues ?? [])
            db.FurniIntEntries.Add(Linked(new FurniIntEntry { Position = position++, Value = value }));

        if (data.VoteResult is { } result)
            db.FurniVoteResults.Add(Linked(new FurniVoteResult { Result = result }));

        if (data.Hits is not null || data.Target is not null)
            db.FurniCrackables.Add(Linked(new FurniCrackable { Hits = data.Hits ?? 0, Target = data.Target ?? 0 }));

        if (data.ScoreType is not null || data.HighScoreEntries is { Count: > 0 })
        {
            var board = Linked(new FurniHighScore
            {
                ScoreType = data.ScoreType ?? 0,
                ClearType = data.ClearType ?? 0,
            });

            position = 0;
            foreach (var row in data.HighScoreEntries ?? [])
            {
                var entry = new FurniHighScoreEntry { Position = position++, Score = row.Score, RecordedAt = row.RecordedAt };
                var userPosition = 0;
                foreach (var name in row.Users)
                    entry.Users.Add(new FurniHighScoreUser { Position = userPosition++, Username = name });
                board.Entries.Add(entry);
            }

            db.FurniHighScores.Add(board);
        }
    }

    private static FurniDataView HighScore(FurniDataView view, string state, FurniHighScore? board) => view with
    {
        State = state,
        ScoreType = board?.ScoreType ?? 0,
        ClearType = board?.ClearType ?? 0,
        HighScoreEntries = board?.Entries.OrderBy(e => e.Position)
            .Select(e => new FurniHighScoreEntryView(
                e.Score, e.Users.OrderBy(u => u.Position).Select(u => u.Username).ToList(), e.RecordedAt))
            .ToList() ?? [],
    };
}
