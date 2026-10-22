using Habbo.Domain.Catalog;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Silo.Abstractions.Catalog;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;
using Orleans;

namespace Habbo.Silo.Catalog;

/// <summary>Targeted offers: the next one a player has not bought up and that is still running, shown
/// the way the client last left it; bought at its own price up to its limit.</summary>
public sealed class TargetedOfferService(
    IDbContextFactory<AppDbContext> dbFactory,
    IGrainFactory grains,
    IShopCatalog catalog,
    PlayerMessenger messenger) : ITargetedOfferService
{
    /// <summary>The state an offer the player has never seen is shown in.</summary>
    private const int NotSeen = 0;

    public async Task SendAsync(PlayerData player, int offerId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await SendAsync(db, player, await OpenOffers(db, player).FirstOrDefaultAsync(o => o.Id == offerId));
    }

    public async Task SendNextAsync(PlayerData player)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await SendAsync(db, player, await OpenOffers(db, player).OrderBy(o => o.SortOrder).ThenBy(o => o.Id).FirstOrDefaultAsync());
    }

    /// <summary>The offers still running that the player may still buy.</summary>
    private static IQueryable<TargetedOffer> OpenOffers(AppDbContext db, PlayerData player)
    {
        var now = DateTimeOffset.UtcNow;
        return db.TargetedOffers.AsNoTracking().Where(o => (o.ExpiresAt == null || o.ExpiresAt > now)
            && (o.PurchaseLimit == 0 || !db.TargetedOfferStates.Any(s => s.UserId == player.Id && s.TargetedOfferId == o.Id && s.Purchased >= o.PurchaseLimit)));
    }

    private async Task SendAsync(AppDbContext db, PlayerData player, TargetedOffer? offer)
    {
        if (offer is null || await catalog.GetOfferAsync(offer.OfferId) is not { } catalogOffer)
        {
            await messenger.SendAsync(player.Id, new TargetedOfferNotFoundComposer());
            return;
        }

        var state = await db.TargetedOfferStates.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == player.Id && s.TargetedOfferId == offer.Id);
        var seconds = offer.ExpiresAt is { } end ? (int)Math.Max(0, (end - DateTimeOffset.UtcNow).TotalSeconds) : 0;
        await messenger.SendAsync(player.Id, new TargetedOfferComposer(new TargetedOfferData(
            state?.TrackingState ?? NotSeen, offer.Id, offer.Identifier, catalogOffer.LocalizationId, offer.PriceInCredits,
            offer.PriceInActivityPoints, offer.ActivityPointType,
            offer.PurchaseLimit == 0 ? 0 : offer.PurchaseLimit - (state?.Purchased ?? 0),
            seconds, offer.Title, offer.Description, offer.ImageUrl, offer.IconImageUrl, offer.Type, offer.SubProductCodes)));
    }

    public async Task<PurchaseResult> PurchaseAsync(PlayerData player, int offerId, int quantity)
    {
        // under a lock on the player's targeted offers until the purchase is counted, so two at once cannot
        // both pass the purchase limit
        await using var db = await dbFactory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await PlayerLock.TakeAsync(db, player.Id, nameof(TargetedOfferService));
        var offer = await OpenOffers(db, player).FirstOrDefaultAsync(o => o.Id == offerId);
        var state = await db.TargetedOfferStates.FirstOrDefaultAsync(s => s.UserId == player.Id && s.TargetedOfferId == offerId);
        if (offer is null || quantity < 1 || (offer.PurchaseLimit > 0 && (long)(state?.Purchased ?? 0) + quantity > offer.PurchaseLimit)
            || await catalog.GetOfferAsync(offer.OfferId) is not { } catalogOffer)
            return PurchaseResult.Failed(PurchaseFailure.NotAllowed);

        var priced = catalogOffer with
        {
            PriceInCredits = offer.PriceInCredits, PriceInActivityPoints = offer.PriceInActivityPoints,
            ActivityPointType = offer.ActivityPointType, MaxAmount = Math.Max(catalogOffer.MaxAmount, quantity),
        };
        var playerGrain = grains.GetGrain<IPlayerGrain>(player.Id);
        var result = await playerGrain.PurchaseAsync(priced, quantity, string.Empty);
        if (!result.Success)
            return result;

        if (state is null)
            db.TargetedOfferStates.Add(state = new TargetedOfferState { UserId = player.Id, TargetedOfferId = offerId });
        state.Purchased += quantity;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        await playerGrain.AnnounceItemsAsync(result.ItemIds ?? []);
        return result;
    }

    public async Task SetStateAsync(PlayerData player, int offerId, int trackingState)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await db.TargetedOffers.AnyAsync(o => o.Id == offerId))
            return;

        var state = await db.TargetedOfferStates.FirstOrDefaultAsync(s => s.UserId == player.Id && s.TargetedOfferId == offerId);
        if (state is null)
            db.TargetedOfferStates.Add(state = new TargetedOfferState { UserId = player.Id, TargetedOfferId = offerId });
        state.TrackingState = trackingState;
        await db.SaveChangesAsync();
    }
}
