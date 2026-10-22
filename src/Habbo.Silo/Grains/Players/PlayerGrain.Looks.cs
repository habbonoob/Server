using Habbo.Domain.Users;
using Habbo.Protocol.Messages.Outgoing.Avatar;
using Habbo.Protocol.Messages.Outgoing.Inventory.Clothing;
using Habbo.Silo.Abstractions.Players;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Players;

/// <summary>How the player looks: figure, motto, the wardrobe and the clothing they unlocked.</summary>
public sealed partial class PlayerGrain
{
    public async Task<PlayerData?> UpdateFigureAsync(string figure, Gender gender)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await LoadUserAsync(db);
        if (!await figures.IsValidAsync(figure, gender, user.ClubLevel, await UnlockedSetsAsync()))
            return null;

        user.Figure = figure;
        user.Gender = gender;
        await db.SaveChangesAsync();

        Send(new FigureUpdateComposer(figure, gender.ToLetter()));
        await ReachAchievementAsync(Achievements.AchievementNames.AvatarLooks, 1);
        return Remember(user);
    }

    public async Task<PlayerData> UpdateMottoAsync(string motto)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await LoadUserAsync(db);
        var max = accountOptions.Value.MottoMaxLength;
        motto = (await wordFilter.FilterAsync(motto.Length > max ? motto[..max] : motto)).Text;
        user.Motto = motto.Length > max ? motto[..max] : motto;
        await db.SaveChangesAsync();
        return Remember(user);
    }

    public async Task SendWardrobeAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await LoadUserAsync(db);
        var outfits = await db.UserOutfits.AsNoTracking()
            .Where(o => o.UserId == PlayerId)
            .OrderBy(o => o.Slot)
            .ToListAsync();

        Send(new WardrobeMessageComposer(
            (int)user.ClubLevel,
            outfits.Where(o => MayUseSlot(user.ClubLevel, o.Slot))
                .Select(o => new OutfitData(o.Slot, o.Figure, o.Gender.ToLetter()))
                .ToList()));
    }

    public async Task SaveOutfitAsync(int slot, string figure, Gender gender)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await LoadUserAsync(db);
        if (!MayUseSlot(user.ClubLevel, slot)
            || !await figures.IsValidAsync(figure, gender, user.ClubLevel, await UnlockedSetsAsync()))
            return;

        var outfit = await db.UserOutfits.FirstOrDefaultAsync(o => o.UserId == PlayerId && o.Slot == slot);
        if (outfit is null)
        {
            outfit = new UserOutfit { UserId = PlayerId, Slot = slot, Figure = figure };
            db.UserOutfits.Add(outfit);
        }

        outfit.Figure = figure;
        outfit.Gender = gender;
        await db.SaveChangesAsync();
    }

    /// <summary>The club's slots are the first five, VIP's the rest, as the avatar editor unlocks them.</summary>
    private static bool MayUseSlot(ClubLevel club, int slot) => slot switch
    {
        < UserOutfit.FirstSlot or > UserOutfit.LastSlot => false,
        <= UserOutfit.LastClubSlot => club >= ClubLevel.Club,
        _ => club >= ClubLevel.Vip,
    };

    public async Task SendFigureSetsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var sets = await db.UserFigureSets.AsNoTracking().Where(s => s.UserId == PlayerId).OrderBy(s => s.SetId).ToListAsync();
        Send(new FigureSetIdsComposer(
            sets.Select(s => s.SetId).ToList(),
            sets.Select(s => s.FurniClassName).Where(name => name.Length > 0).Distinct().ToList()));
    }

    public async Task<IReadOnlyList<int>> UnlockFigureSetsAsync(IReadOnlyList<int> setIds, string furniClassName)
    {
        var known = await figures.KnownSetsAsync(setIds);
        await using var db = await dbFactory.CreateDbContextAsync();
        var owned = await db.UserFigureSets.Where(s => s.UserId == PlayerId).Select(s => s.SetId).ToListAsync();
        var added = known.Except(owned).ToList();

        foreach (var setId in added)
            db.UserFigureSets.Add(new UserFigureSet { UserId = PlayerId, SetId = setId, FurniClassName = furniClassName });
        await db.SaveChangesAsync();

        foreach (var setId in added)
            Send(new FigureSetIdAddedComposer(setId));
        return added;
    }

    private async Task<IReadOnlySet<int>> UnlockedSetsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return (await db.UserFigureSets.AsNoTracking().Where(s => s.UserId == PlayerId).Select(s => s.SetId).ToListAsync())
            .ToHashSet();
    }
}
