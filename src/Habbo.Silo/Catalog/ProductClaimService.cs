using Habbo.Domain.Catalog;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.PeerService;
using Habbo.Silo.Abstractions.Catalog;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;
using Orleans;

namespace Habbo.Silo.Catalog;

/// <summary>Free products (seed/product_claims.json): each claimed once per player for the furniture it names.</summary>
public sealed class ProductClaimService(IDbContextFactory<AppDbContext> dbFactory, IGrainFactory grains, PlayerMessenger messenger)
    : IProductClaimService
{
    public async Task SendHasClaimedAsync(PlayerData player, string claimId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var claimed = await db.ProductClaimRecords.AnyAsync(r => r.UserId == player.Id && r.ClaimId == claimId);
        await messenger.SendAsync(player.Id, new HasClaimedProductResponseMessageComposer(claimId, claimed));
    }

    public async Task ClaimAsync(PlayerData player, string claimId)
    {
        var result = await TryClaimAsync(player, claimId);
        await messenger.SendAsync(player.Id, new ClaimProductResultMessageComposer(claimId, result));
    }

    private async Task<int> TryClaimAsync(PlayerData player, string claimId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.ProductClaims.AsNoTracking().FirstOrDefaultAsync(c => c.Id == claimId) is not { } claim)
            return ClaimProductResultMessageComposer.NotFound;
        if (!claim.Active)
            return ClaimProductResultMessageComposer.NotActive;

        var definitionIds = await db.ItemDefinitions.Where(d => claim.Furni.Contains(d.ClassName)).ToDictionaryAsync(d => d.ClassName, d => d.Id);
        if (claim.Furni.Any(className => !definitionIds.ContainsKey(className)))
            return ClaimProductResultMessageComposer.Failed;

        // recorded first, so claiming twice at once never hands out twice
        db.ProductClaimRecords.Add(new ProductClaimRecord { UserId = player.Id, ClaimId = claimId });
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return ClaimProductResultMessageComposer.AlreadyClaimed;
        }

        var playerGrain = grains.GetGrain<IPlayerGrain>(player.Id);
        foreach (var className in claim.Furni)
            await playerGrain.ReceiveFurnitureAsync(definitionIds[className], string.Empty, 1);
        return ClaimProductResultMessageComposer.Claimed;
    }
}
