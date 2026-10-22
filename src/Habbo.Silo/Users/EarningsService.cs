using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Vault;
using Habbo.Silo.Abstractions.Players;
using Microsoft.EntityFrameworkCore;
using Orleans;

namespace Habbo.Silo.Users;

/// <summary>What players have earned and not yet claimed, by the earnings window's categories: credits and duckets
/// paid into the purse on claiming, products handed over. The credit vault holds back credits bought with real
/// money until enough are bought; this hotel sells none, so every vault is open and empty.</summary>
public sealed class EarningsService(IDbContextFactory<AppDbContext> dbFactory, IGrainFactory grains, PlayerMessenger messenger)
    : IEarningsService
{
    /// <summary>The activity point type duckets are.</summary>
    private const int DucketsType = 0;

    public async Task SendStatusAsync(PlayerData player)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var waiting = await db.IncomeRewards.AsNoTracking().Where(r => r.UserId == player.Id).ToListAsync();

        // credits and duckets add up per category; each product stands on its own
        var totals = waiting.Where(r => r.ProductCode.Length == 0).GroupBy(r => (r.Category, r.RewardType))
            .Select(g => new IncomeRewardData(g.Key.Category, g.Key.RewardType, g.Sum(r => r.Amount), string.Empty));
        var products = waiting.Where(r => r.ProductCode.Length > 0)
            .Select(r => new IncomeRewardData(r.Category, r.RewardType, r.Amount, r.ProductCode));
        await messenger.SendAsync(player.Id, new IncomeRewardStatusMessageComposer(totals.Concat(products).ToList()));
    }

    public async Task ClaimAsync(PlayerData player, byte category)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var claimed = await db.IncomeRewards
            .Where(r => r.UserId == player.Id && (category == IncomeRewardData.AllCategories || r.Category == category)).ToListAsync();
        if (claimed.Count == 0)
        {
            await messenger.SendAsync(player.Id, new IncomeRewardClaimResponseMessageComposer(category, false));
            return;
        }

        db.IncomeRewards.RemoveRange(claimed);
        await db.SaveChangesAsync();

        var playerGrain = grains.GetGrain<IPlayerGrain>(player.Id);
        var credits = claimed.Where(r => r.ProductCode.Length == 0 && r.RewardType == IncomeRewardData.Credits).Sum(r => r.Amount);
        var duckets = claimed.Where(r => r.ProductCode.Length == 0 && r.RewardType == IncomeRewardData.Duckets).Sum(r => r.Amount);
        if (credits > 0)
            await messenger.SendAsync(player.Id, new Habbo.Protocol.Messages.Outgoing.Inventory.Purse.CreditBalanceComposer(
                await playerGrain.AddCreditsAsync(credits, CurrencyTransactionReasons.Earnings)));
        if (duckets > 0)
            await playerGrain.AddActivityPointsAsync(DucketsType, duckets, CurrencyTransactionReasons.Earnings);

        foreach (var product in claimed.Where(r => r.ProductCode.Length > 0))
        {
            if (await db.ItemDefinitions.AsNoTracking().Where(d => d.ClassName == product.ProductCode).Select(d => (int?)d.Id).FirstOrDefaultAsync()
                is { } definitionId)
            {
                await playerGrain.ReceiveFurnitureAsync(definitionId, string.Empty, Math.Max(1, product.Amount));
            }
        }

        await messenger.SendAsync(player.Id, new IncomeRewardClaimResponseMessageComposer(category, true));
        await SendStatusAsync(player);
    }

    public Task SendVaultStatusAsync(PlayerData player) =>
        messenger.SendAsync(player.Id, new CreditVaultStatusMessageComposer(true, 0, 0));

    /// <summary>There is nothing held back to take out; the window is told as much.</summary>
    public Task WithdrawVaultAsync(PlayerData player) => SendVaultStatusAsync(player);

    public async Task EarnAsync(Guid playerId, byte category, byte rewardType, int amount)
    {
        if (amount <= 0)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        db.IncomeRewards.Add(new IncomeReward { UserId = playerId, Category = category, RewardType = rewardType, Amount = amount });
        await db.SaveChangesAsync();
        await NotifyAsync(playerId, category);
    }

    public Task NotifyAsync(Guid playerId, byte category) =>
        messenger.SendAsync(playerId, new IncomeRewardNotificationMessageComposer(category));
}
