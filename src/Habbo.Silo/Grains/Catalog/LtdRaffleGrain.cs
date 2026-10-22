using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Protocol.Messages.Outgoing.Inventory.Purse;
using Habbo.Silo.Abstractions.Catalog;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Achievements;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;

namespace Habbo.Silo.Grains.Catalog;

/// <summary>One limited edition's raffle. Entries are kept until the raffle closes; then the entrants are taken in a
/// random order and each is sold the edition as a purchase would, until none are left — whoever cannot pay by then, or
/// comes after the last one, loses. Everyone is told how it went.</summary>
public sealed class LtdRaffleGrain(
    IOptions<CatalogExtrasOptions> options,
    IDbContextFactory<AppDbContext> dbFactory,
    PlayerMessenger messenger,
    ILogger<LtdRaffleGrain> logger) : Grain, ILtdRaffleGrain
{
    /// <summary>Each entrant, with what came with their purchase (a pet's name, say).</summary>
    private readonly Dictionary<Guid, string> _entrants = [];

    private ShopOffer? _offer;
    private IGrainTimer? _draw;
    private bool _drawn;

    public Task<bool> IsOpenAsync(ShopOffer offer) => Task.FromResult(ClosesAt(offer, DateTimeOffset.UtcNow) is not null);

    /// <summary>When the offer's raffle closes, if it is open now; null when it is not.</summary>
    private DateTimeOffset? ClosesAt(ShopOffer offer, DateTimeOffset now)
    {
        var window = TimeSpan.FromSeconds(options.Value.LtdRaffleSeconds);
        if (_drawn || window <= TimeSpan.Zero || !offer.IsLimited || offer.AvailableFrom is not { } opens)
            return null;

        var closes = opens + window;
        return now >= opens && now < closes ? closes : null;
    }

    public async Task<bool> EnterAsync(Guid playerId, ShopOffer offer, string extraData)
    {
        var now = DateTimeOffset.UtcNow;
        if (ClosesAt(offer, now) is not { } closes)
            return false;

        _offer = offer;
        _entrants.TryAdd(playerId, extraData);
        _draw ??= this.RegisterGrainTimer(async _ => await DrawAsync(),
            new GrainTimerCreationOptions { DueTime = closes - now, Period = Timeout.InfiniteTimeSpan });
        await messenger.SendAsync(playerId, new LtdRaffleEnteredMessageComposer(await ClassNameAsync(offer)));
        return true;
    }

    private async Task DrawAsync()
    {
        _draw?.Dispose();
        _drawn = true;
        if (_offer is not { } offer)
            return;

        var className = await ClassNameAsync(offer);
        var won = 0;
        foreach (var (playerId, extraData) in _entrants.OrderBy(_ => Random.Shared.Next()).ToList())
        {
            var player = GrainFactory.GetGrain<IPlayerGrain>(playerId);
            var result = await player.PurchaseAsync(offer, 1, extraData);
            if (result.Success)
            {
                won++;
                // won in the raffle of its first moments
                await player.ProgressAchievementAsync(AchievementNames.LtdEarlyBird, 1);
                await messenger.SendAsync(playerId, new CreditBalanceComposer(result.RemainingCredits));
                await messenger.SendAsync(playerId, new PurchaseOKMessageComposer(PurchaseOfferData.From(CatalogOfferDataFactory.From(offer)), result.ItemIds ?? []));
                await player.AnnounceItemsAsync(result.ItemIds ?? []);
            }
            else
                logger.LogInformation("Player {PlayerId} lost the raffle for offer {Offer} ({Failure})", playerId, offer.OfferId, result.Failure);

            await messenger.SendAsync(playerId, new LtdRaffleResultMessageComposer(className,
                result.Success ? LtdRaffleResultMessageComposer.Won : LtdRaffleResultMessageComposer.Lost));
        }

        logger.LogInformation("The raffle for offer {Offer} is drawn: {Won} of {Entrants} entrant(s) got one", offer.OfferId, won, _entrants.Count);
        _entrants.Clear();
    }

    /// <summary>The class name of the furniture the edition is.</summary>
    private async Task<string> ClassNameAsync(ShopOffer offer)
    {
        if (offer.Products.FirstOrDefault(p => p.DefinitionId is not null)?.DefinitionId is not { } definitionId)
            return offer.LocalizationId;

        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.ItemDefinitions.AsNoTracking().Where(d => d.Id == definitionId).Select(d => d.ClassName).FirstOrDefaultAsync()
               ?? offer.LocalizationId;
    }
}
