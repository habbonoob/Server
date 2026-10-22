using System.Text.RegularExpressions;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Protocol.Messages.Outgoing.Inventory.Pets;
using Habbo.Protocol.Messages.Outgoing.Users;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Pets;

/// <summary>Pets outside the rooms: the inventory, what the shop sells, and names.</summary>
public sealed class PetService(IDbContextFactory<AppDbContext> dbFactory, PlayerMessenger messenger, PetNames names) : IPetService
{
    /// <summary>The kind of name the client asks about when it names a pet.</summary>
    public const int PetNameType = 1;

    /// <summary>The whole inventory goes in one fragment.</summary>
    private const int Fragments = 1;
    private const int FirstFragment = 0;

    /// <summary>The client reads a pet offer's kind off the end of its product code, as in "a0 pet15".</summary>
    private static readonly Regex TrailingNumber = new(@"(\d+)$");

    public async Task SendInventoryAsync(PlayerData player)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var pets = await db.Pets.AsNoTracking().Where(p => p.OwnerId == player.Id && p.RoomId == null).OrderBy(p => p.Id).ToListAsync();
        await messenger.SendAsync(player.Id, new PetInventoryComposer(Fragments, FirstFragment, pets.Select(PetData.From).ToList()));
    }

    public async Task SendSellablePalettesAsync(PlayerData player, string productCode)
    {
        var palettes = new List<SellablePetPaletteData>();
        if (TrailingNumber.Match(productCode) is { Success: true } match && int.TryParse(match.Value, out var typeId))
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            palettes = await db.PetPalettes.AsNoTracking().Where(p => p.TypeId == typeId).OrderBy(p => p.PaletteId)
                .Select(p => new SellablePetPaletteData(p.TypeId, p.PaletteId, p.PaletteId, p.Sellable, p.Rare)).ToListAsync();
        }

        await messenger.SendAsync(player.Id, new SellablePetPalettesMessageComposer(productCode, palettes));
    }

    public async Task ApproveNameAsync(PlayerData player, string name, int type)
    {
        if (type != PetNameType)
            return;

        var (result, info) = await names.CheckAsync(name);
        await messenger.SendAsync(player.Id, new ApproveNameMessageComposer(result, info));
    }
}
