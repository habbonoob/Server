using Habbo.Domain.Furniture;
using Habbo.Domain.RewardTracks;
using Habbo.Domain.Users;
using Habbo.Protocol.Messages.Outgoing.RewardTrack;
using Habbo.Protocol.Messages.Outgoing.Inventory.Purse;
using Habbo.Silo.Abstractions.Catalog;
using Habbo.Silo.Catalog;
using Habbo.Silo.RewardTracks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Habbo.Silo.Grains.Players;

/// <summary>Reward tracks: tasks counting what the player does earn points, the points unlock prizes they claim, and
/// a track's premium side, bought with credits and diamonds, boosts the points and opens premium prizes.</summary>
public sealed partial class PlayerGrain
{
    /// <summary>Diamonds, as the activity point type premium is paid in.</summary>
    private const int Diamonds = 5;

    private RewardTrackCatalog RewardTracks => ServiceProvider.GetRequiredService<RewardTrackCatalog>();

    public async Task SendRewardTracksAsync()
    {
        var tracks = await RewardTracks.AllAsync();
        await using var db = await dbFactory.CreateDbContextAsync();
        var progress = await RewardTrackProgressAsync(db);
        Send(new RewardTracksComposer(Disabled: false,
            tracks.Select(track => RewardTrackRules.ToWire(track, progress.GetValueOrDefault(track.Id) ?? NewProgress(track))).ToList(),
            Reload: false));
    }

    public async Task TrackActionAsync(string action, string parameter = "", int amount = 1, int effectId = 0)
    {
        // the seasonal quests that count these; the quest service pays through this grain, so it is not awaited here
        if (action is RewardTrackActions.Wave or RewardTrackActions.Dance or RewardTrackActions.Swim)
            ServiceProvider.GetRequiredService<Habbo.Silo.Abstractions.Quests.ISeasonalQuestService>().ProgressAsync(PlayerId, action, parameter, amount).Ignore();
        await CountDailyTasksAsync(action, parameter, amount, effectId);

        // most actions count on no track: no reading for them
        var tracks = (await RewardTracks.AllAsync()).Where(t => t.Tasks.Any(task => task.Action == action)).ToList();
        if (tracks.Count == 0 || amount <= 0)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var progress = await RewardTrackProgressAsync(db, track: true);
        var told = new List<RewardTrackProgressComposer>();
        foreach (var track in tracks)
        {
            var row = progress.GetValueOrDefault(track.Id) ?? AddProgress(db, track);
            foreach (var counted in RewardTrackRules.Count(track, row, action, parameter, amount))
                told.Add(new RewardTrackProgressComposer(track.Id, counted.Task.Id, counted.Count, row.Points));
        }

        if (told.Count == 0)
            return;
        await db.SaveChangesAsync();
        foreach (var message in told)
            Send(message);
    }

    public async Task ProgressRewardTrackTaskAsync(string trackId, string taskId, int score, bool addToExisting)
    {
        if (await RewardTracks.FindAsync(trackId) is not { } track || track.Tasks.FirstOrDefault(t => t.Id == taskId) is not { } task)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var row = (await RewardTrackProgressAsync(db, track: true)).GetValueOrDefault(track.Id) ?? AddProgress(db, track);
        var count = addToExisting ? RewardTrackRules.CountOf(row, task) + score : score;
        if (RewardTrackRules.SetCount(track, row, task, count) is not { } counted)
            return;

        await db.SaveChangesAsync();
        Send(new RewardTrackProgressComposer(track.Id, task.Id, counted.Count, row.Points));
    }

    public async Task ResetRewardTrackAsync(string trackId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        // premium was paid for, and stays
        if (await db.RewardTrackProgresses.Where(p => p.UserId == PlayerId && p.TrackId == trackId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Points, 0)
                    .SetProperty(p => p.Tasks, new Dictionary<string, int>())
                    .SetProperty(p => p.Claimed, new List<string>())) > 0)
            await SendRewardTracksAsync();
    }

    public async Task ClaimRewardTrackPrizeAsync(string trackId, string prizeId)
    {
        if (await RewardTracks.FindAsync(trackId) is not { } track)
        {
            Send(new RewardTrackClaimResultComposer(trackId, prizeId, RewardTrackClaimResultComposer.NoSuchTrack));
            return;
        }

        if (track.Prizes.FirstOrDefault(p => p.Id == prizeId) is not { } prize)
        {
            Send(new RewardTrackClaimResultComposer(trackId, prizeId, RewardTrackClaimResultComposer.NoSuchPrize));
            return;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var row = (await RewardTrackProgressAsync(db, track: true)).GetValueOrDefault(track.Id) ?? NewProgress(track);
        var refusal = row.Claimed.Contains(prize.Id) ? RewardTrackClaimResultComposer.AlreadyClaimed
            : prize.Premium && !row.Premium ? RewardTrackClaimResultComposer.PremiumRequired
            : row.Points < prize.Points ? RewardTrackClaimResultComposer.NotEnoughPoints
            : (int?)null;
        if (refusal is { } code)
        {
            Send(new RewardTrackClaimResultComposer(trackId, prizeId, code));
            return;
        }

        // the prize and the claim together: neither without the other
        await using var transaction = await db.Database.BeginTransactionAsync();
        var granted = await GrantPrizeAsync(db, new ProductPrize(prize.Type, prize.Item, prize.Extra, prize.Amount), CurrencyTransactionReasons.RewardTrackPrize);
        if (granted is null)
        {
            Send(new RewardTrackClaimResultComposer(trackId, prizeId, RewardTrackClaimResultComposer.Failed));
            return;
        }

        if (db.Entry(row).State == EntityState.Detached)
            db.RewardTrackProgresses.Add(row);
        row.Claimed = [.. row.Claimed, prize.Id];
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        await granted();
        Send(new RewardTrackClaimResultComposer(trackId, prizeId, RewardTrackClaimResultComposer.Claimed));
    }

    public async Task PurchaseRewardTrackPremiumAsync(string trackId)
    {
        if (await RewardTracks.FindAsync(trackId) is not { } track)
        {
            Send(new RewardTrackPremiumPurchaseResultComposer(trackId, RewardTrackPremiumPurchaseResultComposer.NoSuchTrack, 0));
            return;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var row = (await RewardTrackProgressAsync(db, track: true)).GetValueOrDefault(track.Id) ?? AddProgress(db, track);
        var refusal = track.Premium is not { } premium ? RewardTrackPremiumPurchaseResultComposer.NotSold
            : row.Premium ? RewardTrackPremiumPurchaseResultComposer.AlreadyOwned
            : premium.CostCredits < 0 || premium.CostDiamonds < 0 ? RewardTrackPremiumPurchaseResultComposer.InvalidPrice
            : (int?)null;
        if (refusal is { } code || await db.Users.FirstOrDefaultAsync(u => u.Id == PlayerId) is not { } user)
        {
            Send(new RewardTrackPremiumPurchaseResultComposer(trackId, refusal ?? RewardTrackPremiumPurchaseResultComposer.Failed, row.Points));
            return;
        }

        // charged as a catalogue offer of credits and diamonds is
        var price = new ShopOffer(0, string.Empty, track.Premium!.CostCredits, ClubLevel.None, 1, [], track.Premium.CostDiamonds, Diamonds, 0,
            false, false, false, string.Empty);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var charge = await ChargeAsync(db, user, price, amount: 1, extraCredits: 0);
        if (charge.Failure != PurchaseFailure.None)
        {
            Send(new RewardTrackPremiumPurchaseResultComposer(trackId, charge.Failure == PurchaseFailure.NotEnoughCredits
                ? RewardTrackPremiumPurchaseResultComposer.NotEnoughCredits
                : RewardTrackPremiumPurchaseResultComposer.NotEnoughDiamonds, row.Points));
            return;
        }

        row.Premium = true;
        row.Points += track.Premium.InstantPoints;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        await AnnounceChargeAsync(user, price, charge);
        Send(new CreditBalanceComposer(user.Credits));
        Send(new RewardTrackPremiumPurchaseResultComposer(trackId, RewardTrackPremiumPurchaseResultComposer.Bought, row.Points));
    }

    /// <summary>Hands a reward track prize or a daily task's reward over in the transaction; what to tell the player once
    /// it is saved, or null when the hotel has no such thing to give.</summary>
    private async Task<Func<Task>?> GrantPrizeAsync(Habbo.Infrastructure.AppDbContext db, ProductPrize prize, string reason)
    {
        if (prize.Type == RewardTrackPrizeTypes.Currency)
        {
            if (!int.TryParse(prize.Item, out var currency))
                return null;
            return async () => await AddActivityPointsAsync(currency, prize.Amount, reason);
        }

        // the Habbicon service tells this grain of the achievements it counts: not awaited from inside it
        if (prize.Type == RewardTrackPrizeTypes.Habbicon)
        {
            if (!int.TryParse(prize.Item, out var habbiconId))
                return null;
            return () =>
            {
                ServiceProvider.GetRequiredService<Habbo.Silo.Abstractions.Habbicons.IHabbiconService>().GrantAsync(PlayerId, habbiconId).Ignore();
                return Task.CompletedTask;
            };
        }

        if (await PrizeProductAsync(db, prize) is not var (product, extraData))
            return null;

        var user = await LoadUserAsync(db);
        var granted = await GrantProductsAsync(db, PrizeOffer(product), 1, extraData, user, serial: null);
        return async () =>
        {
            await AnnounceItemsAsync(granted.Created.Select(i => i.Id).ToList());
            await AnnounceGrantAsync(granted);
        };
    }

    private static ShopOffer PrizeOffer(ShopProduct product) => new(0, string.Empty, 0, ClubLevel.None, 1, [product], 0, 0, 0,
        false, false, false, string.Empty);

    /// <summary>The prize as a shop product, and the order data its kind needs (a pet's name and colours); null for a
    /// kind or an item the hotel does not have.</summary>
    private static async Task<(ShopProduct Product, string ExtraData)?> PrizeProductAsync(Habbo.Infrastructure.AppDbContext db, ProductPrize prize)
    {
        var amount = Math.Max(1, prize.Amount);
        int.TryParse(prize.Item, out var id);
        switch (prize.Type)
        {
            case RewardTrackPrizeTypes.FloorFurni or RewardTrackPrizeTypes.WallFurni:
                var type = prize.Type == RewardTrackPrizeTypes.WallFurni ? ItemType.Wall : ItemType.Floor;
                var definition = await db.ItemDefinitions.AsNoTracking().FirstOrDefaultAsync(d => d.ClassId == id && d.Type == type);
                return definition is null ? null : (new ShopProduct(definition.Id, definition.ClassId, type == ItemType.Wall, prize.Extra, amount), string.Empty);

            case RewardTrackPrizeTypes.Badge:
                return (new ShopProduct(null, 0, false, prize.Item, 1, ProductTypes.Badge), string.Empty);

            case RewardTrackPrizeTypes.Effect:
                return (new ShopProduct(null, 0, false, string.Empty, amount, ProductTypes.Effect, ClassId: id), string.Empty);

            case RewardTrackPrizeTypes.ChatStyle:
                return (new ShopProduct(null, 0, false, string.Empty, 1, ProductTypes.ChatStyle, ClassId: id), string.Empty);

            case RewardTrackPrizeTypes.Bot:
                var bot = await db.BotTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Code == prize.Item.ToLowerInvariant());
                return bot is null ? null : (new ShopProduct(null, 0, false, prize.Extra, amount, ProductTypes.Bot, ClassId: bot.Id), string.Empty);

            case RewardTrackPrizeTypes.Pet:
                // "type palette colour"; the pet is named for its kind, as the client names a pet nobody named
                var parts = prize.Extra.Split(' ');
                var pet = await db.PetTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
                if (pet is null || parts.Length < 3)
                    return null;
                var name = pet.Code.Length == 0 ? pet.Code : char.ToUpperInvariant(pet.Code[0]) + pet.Code[1..];
                return (new ShopProduct(null, 0, false, string.Empty, 1, ProductTypes.Pet, ClassId: pet.Id), $"{name}\n{parts[1]}\n{parts[2]}");

            default:
                return null;
        }
    }

    /// <summary>The player's rows, by track; tracked when they are to be changed.</summary>
    private async Task<Dictionary<string, RewardTrackProgress>> RewardTrackProgressAsync(Habbo.Infrastructure.AppDbContext db, bool track = false)
    {
        var rows = db.RewardTrackProgresses.Where(p => p.UserId == PlayerId);
        return await (track ? rows : rows.AsNoTracking()).ToDictionaryAsync(p => p.TrackId);
    }

    private RewardTrackProgress NewProgress(RewardTrack track) => new() { UserId = PlayerId, TrackId = track.Id };

    private RewardTrackProgress AddProgress(Habbo.Infrastructure.AppDbContext db, RewardTrack track)
    {
        var row = NewProgress(track);
        db.RewardTrackProgresses.Add(row);
        return row;
    }
}
