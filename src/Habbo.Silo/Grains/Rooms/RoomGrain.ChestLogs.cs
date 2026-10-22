using System.Globalization;
using Habbo.Domain.Furniture.Chests;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Chests.Storage;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Transactions;
using Habbo.Silo.Chests;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>The chest transaction logs: a chest's, for whoever may look inside it, and the whole room's, for those who
/// may read its wired. The room keeps as many as the client's variables allow; older ones go.</summary>
public sealed partial class RoomGrain
{
    public async Task SendChestLogsAsync(Guid playerId, int chestId, int pageSize, int page)
    {
        if (_entities?.Find(playerId) is not { } user || Chest(chestId) is not { } chest || !MayOpenChest(user, chest))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        await SendChestLogPageAsync(playerId, WiredTransactionLogListMessageComposer.ChestLogs, chestId,
            db.ChestTransactions.Where(t => t.RoomId == _room!.Id && t.ChestIds.Contains(chestId)), pageSize, page);
    }

    public async Task SendRoomChestLogsAsync(Guid playerId, int pageSize, int page)
    {
        if (_entities?.Find(playerId) is not { } user || !WiredRights(user).Read)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        await SendChestLogPageAsync(playerId, WiredTransactionLogListMessageComposer.RoomLogs, _room!.Id,
            db.ChestTransactions.Where(t => t.RoomId == _room.Id), pageSize, page);
    }

    public async Task SendChestLogDetailsAsync(Guid playerId, long transactionId)
    {
        if (_entities?.Find(playerId) is not { } user)
            return;

        // those who may read the room's wired see any; others those of a chest they may look into
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.ChestTransactions.AsNoTracking().Include(t => t.Items).FirstOrDefaultAsync(t => t.Id == transactionId && t.RoomId == _room!.Id)
            is not { } transaction
            || (!WiredRights(user).Read && !transaction.ChestIds.Any(id => Chest(id) is { } chest && MayOpenChest(user, chest))))
            return;

        var names = await NamesOfAsync(db, [transaction]);
        ChestItemAmountData Amount(ChestTransactionItem item) =>
            new(new ChestItemTypeData(item.IsWallItem, item.TypeId, item.PosterId), item.Amount);
        SendTo(playerId, new WiredTransactionLogDetailsMessageComposer(ToLogInfo(transaction, names), transaction.ChestIds,
            transaction.Items.Where(i => i.Deposited).Select(Amount).ToList(), transaction.Items.Where(i => !i.Deposited).Select(Amount).ToList(), false));
    }

    private async Task SendChestLogPageAsync(Guid playerId, int listType, long listId, IQueryable<ChestTransaction> logs, int pageSize, int page)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var size = Math.Clamp(pageSize, 1, MaxWiredLogPageSize);
        var total = await logs.CountAsync();
        // no further than the last page: a huge page number would overflow the offset below into a negative one
        var number = Math.Clamp(page, 1, Math.Max(1, (total + size - 1) / size));
        var found = await logs.AsNoTracking().OrderByDescending(t => t.Id).Skip((number - 1) * size).Take(size).ToListAsync();
        var names = await NamesOfAsync(db, found);
        SendTo(playerId, new WiredTransactionLogListMessageComposer(listType, listId, total, number, size,
            found.Select(t => ToLogInfo(t, names)).ToList()));
    }

    /// <summary>Who each transaction was for: their account id and name.</summary>
    private static async Task<Dictionary<Guid, (int AccountId, string Name)>> NamesOfAsync(Habbo.Infrastructure.AppDbContext db, List<ChestTransaction> transactions)
    {
        var ids = transactions.Select(t => t.UserId).OfType<Guid>().Distinct().ToList();
        return await db.Users.AsNoTracking().Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => (u.AccountId, u.Username));
    }

    private WiredTransactionInfoData ToLogInfo(ChestTransaction transaction, Dictionary<Guid, (int AccountId, string Name)> names)
    {
        var (accountId, name) = transaction.UserId is { } userId && names.TryGetValue(userId, out var who) ? who : (0, string.Empty);
        var readable = _wired is null
            ? transaction.CreatedAt.UtcDateTime.ToString(WiredLogTimeFormat, CultureInfo.InvariantCulture)
            : WiredTime(transaction.CreatedAt);
        return new WiredTransactionInfoData(transaction.Id, transaction.RoomId, transaction.Type, transaction.Info, accountId, name,
            transaction.CreatedAt.ToUnixTimeMilliseconds(), readable, transaction.ChestIds.Count, transaction.WithdrawnFurni,
            transaction.DepositedFurni, transaction.WithdrawnCoins, transaction.DepositedCoins);
    }

    /// <summary>Drops the room's oldest transactions past the most the client's variables say a room keeps.</summary>
    private async Task PruneChestLogsAsync(Habbo.Infrastructure.AppDbContext db)
    {
        var keep = (await chestRules.GetAsync()).MaxLogs;
        if (keep <= 0)
            return;

        var oldestKept = await db.ChestTransactions.Where(t => t.RoomId == _room!.Id).OrderByDescending(t => t.Id).Skip(keep - 1)
            .Select(t => (long?)t.Id).FirstOrDefaultAsync();
        if (oldestKept is { } cutoff)
            await db.ChestTransactions.Where(t => t.RoomId == _room!.Id && t.Id < cutoff).ExecuteDeleteAsync();
    }
}
