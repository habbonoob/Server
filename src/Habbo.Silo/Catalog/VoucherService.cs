using System.Collections.Concurrent;
using Habbo.Domain.Catalog;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Protocol.Messages.Outgoing.Inventory.Purse;
using Habbo.Silo.Abstractions.Catalog;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;
using Orleans;

namespace Habbo.Silo.Catalog;

/// <summary>Redeeming voucher codes: the code must exist, be in date, have uses left and not have been
/// redeemed by this player. Its credits and points are added, and its offer's products handed over free.</summary>
public sealed class VoucherService(
    IDbContextFactory<AppDbContext> dbFactory,
    IGrainFactory grains,
    IShopCatalog catalog,
    PlayerMessenger messenger) : IVoucherService
{
    private const string VoucherReason = "voucher";

    /// <summary>Wrong codes a player may try before a pause, and how long the pause lasts.</summary>
    private const int WrongCodesAllowed = 10;
    private static readonly TimeSpan WrongCodePause = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<Guid, (int Count, DateTimeOffset Since)> _wrongCodes = new();

    private bool MayTry(Guid playerId) =>
        !_wrongCodes.TryGetValue(playerId, out var wrong) || wrong.Count < WrongCodesAllowed || DateTimeOffset.UtcNow - wrong.Since >= WrongCodePause;

    private void Failed(Guid playerId)
    {
        var now = DateTimeOffset.UtcNow;
        _wrongCodes.AddOrUpdate(playerId, (1, now),
            (_, wrong) => now - wrong.Since >= WrongCodePause ? (1, now) : (wrong.Count + 1, wrong.Since));
    }

    public async Task RedeemAsync(PlayerData player, string code)
    {
        code = code.Trim();
        // codes can be guessed: a player who keeps getting them wrong is not let try again for a while
        if (!MayTry(player.Id))
        {
            await messenger.SendAsync(player.Id, new VoucherRedeemErrorMessageComposer(VoucherRedeemErrorMessageComposer.Invalid));
            return;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var now = DateTimeOffset.UtcNow;
        var voucher = code.Length is > 0 and <= Voucher.CodeMaxLength
            ? await db.Vouchers.FirstOrDefaultAsync(v => v.Code == code)
            : null;
        if (voucher is null || voucher.ExpiresAt <= now || (voucher.MaxUses > 0 && voucher.Uses >= voucher.MaxUses)
            || await db.VoucherRedemptions.AnyAsync(r => r.Code == code && r.UserId == player.Id))
        {
            Failed(player.Id);
            await messenger.SendAsync(player.Id, new VoucherRedeemErrorMessageComposer(VoucherRedeemErrorMessageComposer.Invalid));
            return;
        }

        // the player's redemption goes in first, so the same player redeeming twice at once fails on its key rather than
        // using up a second use; then the use is taken, so two players cannot share the last one
        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            db.VoucherRedemptions.Add(new VoucherRedemption { Code = code, UserId = player.Id });
            var redeemed = true;
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                redeemed = false;
            }

            if (!redeemed || await db.Vouchers.Where(v => v.Code == code && (v.MaxUses == 0 || v.Uses < v.MaxUses))
                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.Uses, v => v.Uses + 1)) == 0)
            {
                await transaction.RollbackAsync();
                await messenger.SendAsync(player.Id, new VoucherRedeemErrorMessageComposer(VoucherRedeemErrorMessageComposer.Invalid));
                return;
            }

            await transaction.CommitAsync();
        }

        var playerGrain = grains.GetGrain<IPlayerGrain>(player.Id);
        if (voucher.Credits > 0)
            await messenger.SendAsync(player.Id, new CreditBalanceComposer(await playerGrain.AddCreditsAsync(voucher.Credits, VoucherReason)));
        if (voucher.ActivityPoints > 0)
            await playerGrain.AddActivityPointsAsync(voucher.ActivityPointType, voucher.ActivityPoints, VoucherReason);

        var (name, description) = (string.Empty, string.Empty);
        if (voucher.OfferId is { } offerId && await catalog.GetOfferAsync(offerId) is { } offer)
        {
            var free = offer with { PriceInCredits = 0, PriceInActivityPoints = 0, ClubLevel = ClubLevel.None };
            var result = await playerGrain.PurchaseAsync(free, 1, string.Empty);
            if (result.Success)
            {
                await playerGrain.AnnounceItemsAsync(result.ItemIds ?? []);
                (name, description) = (offer.LocalizationId, offer.LocalizationId);
            }
        }

        await messenger.SendAsync(player.Id, new VoucherRedeemOkMessageComposer(description, name));
    }
}
