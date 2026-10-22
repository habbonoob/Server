using Habbo.Domain.Furniture;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Misc;
using Habbo.Silo.Abstractions.Inventory;
using Habbo.Silo.Abstractions.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orleans;

namespace Habbo.Silo.Users;

/// <summary>Gives a player so many of an item that can be traded — a poster by its number — where the hotel is a
/// sandbox; anywhere else it is not allowed.</summary>
public sealed class SelfDonationService(
    IDbContextFactory<AppDbContext> dbFactory, IGrainFactory grains, PlayerMessenger messenger, IOptions<SelfDonationOptions> options)
    : ISelfDonationService
{
    /// <summary>The poster, the one wall item whose kind is told apart by its number.</summary>
    private const string PosterClassName = "poster";

    public async Task DonateAsync(PlayerData player, bool isWallItem, int typeId, string posterId, int amount)
    {
        var result = await TryDonateAsync(player, isWallItem, typeId, posterId, amount);
        await messenger.SendAsync(player.Id, new SelfDonationResultMessageComposer(result));
    }

    private async Task<int> TryDonateAsync(PlayerData player, bool isWallItem, int typeId, string posterId, int amount)
    {
        var rules = options.Value;
        if (!rules.Enabled)
            return SelfDonationResultMessageComposer.NotAllowed;
        if (amount < 1 || amount > rules.MaxAmount)
            return SelfDonationResultMessageComposer.Failed;

        await using var db = await dbFactory.CreateDbContextAsync();
        var kind = isWallItem ? ItemType.Wall : ItemType.Floor;
        if (await db.ItemDefinitions.AsNoTracking().FirstOrDefaultAsync(d => d.ClassId == typeId && d.Type == kind) is not { Tradeable: true } definition
            || (posterId.Length > 0) != (definition.ClassName == PosterClassName))
            return SelfDonationResultMessageComposer.Failed;

        await grains.GetGrain<IPlayerGrain>(player.Id).ReceiveFurnitureAsync(definition.Id, posterId, amount);
        return SelfDonationResultMessageComposer.Success;
    }
}
