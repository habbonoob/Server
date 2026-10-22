using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Inventory.Purse;
using Habbo.Protocol.Messages.Outgoing.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Players;

/// <summary>The activity-point currencies: duckets, diamonds and the rest.</summary>
public sealed partial class PlayerGrain
{
    public async Task SendActivityPointsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var balances = await BalancesAsync(db);
        Send(new ActivityPointsMessageComposer(
            balances.Select(balance => new ActivityPointData(balance.Type, balance.Amount)).ToList()));
    }

    public async Task<int> GetActivityPointsAsync(int type)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return (await CurrencyAsync(db, type)).Amount;
    }

    public async Task<bool> SpendActivityPointsAsync(int type, int amount, string reason)
    {
        if (amount < 0)
            return false;

        await using var db = await dbFactory.CreateDbContextAsync();
        var currency = await CurrencyAsync(db, type);
        if (currency.Amount < amount)
            return false;

        if (amount == 0)
            return true;

        // taken in the database, not written back as a total: a season reset (or anything else) that changed the
        // balance since it was read is not undone
        await SaveIfNewAsync(db, currency);
        if (await Balance(db, type).Where(c => c.Amount >= amount)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Amount, c => c.Amount - amount)) == 0)
            return false;

        RecordTransaction(db, type, -amount, reason);
        await db.SaveChangesAsync();
        Send(new HabboActivityPointNotificationMessageComposer(await AmountAsync(db, type), -amount, type));
        return true;
    }

    public async Task<int> AddActivityPointsAsync(int type, int amount, string reason)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var currency = await CurrencyAsync(db, type);
        var change = Math.Max(amount, -currency.Amount);
        if (change == 0)
            return currency.Amount;

        // added in the database, never below nothing, for the same reason as spending
        await SaveIfNewAsync(db, currency);
        await Balance(db, type).ExecuteUpdateAsync(s =>
            s.SetProperty(c => c.Amount, c => c.Amount + change < 0 ? 0 : c.Amount + change));
        RecordTransaction(db, type, change, reason);
        await db.SaveChangesAsync();
        var balance = await AmountAsync(db, type);
        Send(new HabboActivityPointNotificationMessageComposer(balance, change, type));
        return balance;
    }

    private IQueryable<UserCurrency> Balance(AppDbContext db, int type) =>
        db.UserCurrencies.Where(c => c.UserId == PlayerId && c.Type == type);

    private Task<int> AmountAsync(AppDbContext db, int type) =>
        Balance(db, type).AsNoTracking().Select(c => c.Amount).FirstAsync();

    /// <summary>A balance the player never had is made first, so the update below has a row to change.</summary>
    private static async Task SaveIfNewAsync(AppDbContext db, UserCurrency currency)
    {
        if (db.Entry(currency).State == EntityState.Added)
            await db.SaveChangesAsync();
    }

    /// <summary>Every currency the hotel runs, and any other the player holds, with a row for each
    /// the player has not touched yet started at the hotel's initial amount.</summary>
    private async Task<List<UserCurrency>> BalancesAsync(AppDbContext db)
    {
        var types = await db.CurrencyTypes.AsNoTracking().ToListAsync();
        var held = await db.UserCurrencies.Where(c => c.UserId == PlayerId).ToListAsync();

        foreach (var type in types.Where(type => held.All(c => c.Type != type.Type)))
        {
            var row = new UserCurrency { UserId = PlayerId, Type = type.Type, Amount = type.InitialAmount };
            db.UserCurrencies.Add(row);
            held.Add(row);
        }

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync();

        return held.OrderBy(c => c.Type).ToList();
    }

    private async Task<UserCurrency> CurrencyAsync(AppDbContext db, int type)
    {
        var currency = await db.UserCurrencies.FirstOrDefaultAsync(c => c.UserId == PlayerId && c.Type == type);
        if (currency is not null)
            return currency;

        var initial = await db.CurrencyTypes.Where(t => t.Type == type).Select(t => t.InitialAmount).FirstOrDefaultAsync();
        currency = new UserCurrency { UserId = PlayerId, Type = type, Amount = initial };
        db.UserCurrencies.Add(currency);
        return currency;
    }

    public async Task<int?> SpendCreditsAsync(int credits, string reason)
    {
        if (credits < 0)
            return null;

        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await LoadUserAsync(db);
        if (user.Credits < credits)
            return null;

        user.Credits -= credits;
        if (credits > 0)
            RecordTransaction(db, CurrencyTransaction.Credits, -credits, reason);
        await db.SaveChangesAsync();
        Remember(user);
        Send(new CreditBalanceComposer(user.Credits));
        return user.Credits;
    }
}
