using Habbo.Domain.Furniture.Data;
using Habbo.Domain.Users;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;
using Habbo.Silo.Users;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Outfits the room's furniture keeps: a mannequin's, and a changing gate's, which dresses
/// those walking through for as long as they stay.</summary>
public sealed partial class RoomGrain
{
    /// <summary>Wears an outfit in this room only, or takes it off (null), for everyone to see.</summary>
    private void DressInRoom(RoomEntityStore.RoomEntity entity, string? outfit)
    {
        entity.RoomOutfit = outfit;
        Broadcast(new UserChangeMessageComposer(
            entity.RoomIndex, entity.Figure, entity.Player.Gender.ToLetter(), entity.Player.Motto, entity.Player.AchievementScore,
            BadgesRank: entity.BadgesRank));
    }

    /// <summary>The furnidata special type of clothing furni: its custom params are the figure sets it unlocks.</summary>
    private const int ClothingSpecialType = 23;

    public async Task BindClothingAsync(Guid playerId, int itemId)
    {
        if (_items?.Find(itemId) is not { } clothing || clothing.Item.OwnerId != playerId || clothing.Temporary
            || clothing.Item.Definition is not { SpecialType: ClothingSpecialType } definition)
            return;

        var sets = (definition.CustomParams ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(set => int.TryParse(set, out var id) ? id : 0).Where(id => id > 0).Distinct().ToList();
        if (sets.Count == 0)
            return;

        // the furni is used up; the clothing stays
        if (!await _furniture!.RemoveAsync(clothing))
            return;

        await GrainFactory.GetGrain<IPlayerGrain>(playerId).UnlockFigureSetsAsync(sets, definition.ClassName);
    }

    public async Task SetClothingChangeDataAsync(Guid playerId, int itemId, string gender, string figure)
    {
        if (!IsController(playerId) || _items?.Find(itemId) is not { Interaction: ClothingChangeInteraction } gate)
            return;

        var outfit = FigureOutfit.Clothing(figure, furnitureOptions.Value.OutfitSetTypes);
        var state = ClothingChangeInteraction.WithOutfit(gate.Data.StateText, GenderExtensions.ParseLetter(gender), outfit);
        await _furniture!.UpdateAsync(gate, gate.Data with { State = state });
    }

    public async Task SetMannequinNameAsync(Guid playerId, int itemId, string name)
    {
        if (!IsController(playerId) || _items?.Find(itemId) is not { Interaction: MannequinInteraction } mannequin)
            return;

        var limit = furnitureOptions.Value.MannequinNameMaxLength;
        name = (await wordFilter.FilterAsync(name.Length > limit ? name[..limit] : name, _filterWords)).Text;
        await _furniture!.UpdateAsync(mannequin, WithMannequin(mannequin.Data, MannequinInteraction.NameKey, name));
    }

    /// <summary>Dresses the mannequin in what the player is wearing: the clothes, not the face.</summary>
    public async Task SetMannequinFigureAsync(Guid playerId, int itemId)
    {
        if (!IsController(playerId) || _entities?.Find(playerId) is not { } entity
            || _items?.Find(itemId) is not { Interaction: MannequinInteraction } mannequin)
            return;

        var outfit = FigureOutfit.Clothing(entity.Player.Figure, furnitureOptions.Value.OutfitSetTypes);
        var data = WithMannequin(mannequin.Data, MannequinInteraction.FigureKey, outfit);
        data = WithMannequin(data, MannequinInteraction.GenderKey, entity.Player.Gender.ToLetter());
        await _furniture!.UpdateAsync(mannequin, data);
    }

    private static FurniDataView WithMannequin(FurniDataView data, string key, string value)
    {
        var entries = (data.MapEntries ?? []).Where(e => e.Key != key).Append(new FurniMapEntryView(key, value)).ToList();
        return data with { Type = (int)FurniDataType.Map, State = null, MapEntries = entries };
    }
}
