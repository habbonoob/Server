using Habbo.Domain.Furniture.Data;
using Habbo.Protocol.Messages.Outgoing.Room.Furniture;
using Habbo.Silo.Items;
using Habbo.Silo.Items.Interactions;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>The room's light: the dimmer's presets and switch, and the background toner's colour.
/// Both answer only to those with rights.</summary>
public sealed partial class RoomGrain
{
    /// <summary>The highest hue, saturation or lightness the toner takes.</summary>
    private const int MaxToneValue = 255;

    private DimmerOptions Dimmer => furnitureOptions.Value.Dimmer;

    public async Task SendDimmerPresetsAsync(Guid playerId, int itemId)
    {
        if (!IsController(playerId) || _items?.Find(itemId) is not { Interaction: RoomDimmerInteraction } dimmer)
            return;

        var state = DimmerStateOf(dimmer);
        SendTo(playerId, new RoomDimmerPresetsMessageComposer(state.PresetId,
            (await DimmerPresetsAsync(itemId)).Select(p => new RoomDimmerPresetData(p.PresetId, p.EffectType, p.ColorHex, p.Brightness)).ToList(),
            state.IsOn, itemId));
    }

    public async Task SaveDimmerPresetAsync(Guid playerId, int itemId, int presetId, int effectType, string colorHex, int brightness, bool apply)
    {
        if (!IsController(playerId) || _items?.Find(itemId) is not { Interaction: RoomDimmerInteraction } dimmer
            || presetId < 1 || presetId > Dimmer.PresetCount
            || !Dimmer.EffectTypes.Contains(effectType)
            || !Dimmer.Colors.Contains(colorHex, StringComparer.OrdinalIgnoreCase)
            || brightness < Dimmer.MinBrightness || brightness > Dimmer.MaxBrightness)
            return;

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var preset = await db.FurniDimmerPresets.FirstOrDefaultAsync(p => p.ItemId == itemId && p.PresetId == presetId);
            if (preset is null)
                db.FurniDimmerPresets.Add(preset = new FurniDimmerPreset { ItemId = itemId, PresetId = presetId, ColorHex = colorHex });
            preset.EffectType = effectType;
            preset.ColorHex = colorHex;
            preset.Brightness = brightness;
            await db.SaveChangesAsync();
        }

        // saving with apply shows the preset at once, and switches the dimmer on
        if (apply)
            await _furniture!.UpdateAsync(dimmer, dimmer.Data with
            {
                State = new DimmerState(true, presetId, effectType, colorHex, brightness).ToString(),
            });
    }

    public async Task ToggleDimmerAsync(Guid playerId, int itemId)
    {
        if (!IsController(playerId) || _items?.Find(itemId) is not { Interaction: RoomDimmerInteraction } dimmer)
            return;

        var state = DimmerStateOf(dimmer);
        var preset = (await DimmerPresetsAsync(itemId)).First(p => p.PresetId == state.PresetId);
        await _furniture!.UpdateAsync(dimmer, dimmer.Data with
        {
            State = new DimmerState(!state.IsOn, preset.PresetId, preset.EffectType, preset.ColorHex, preset.Brightness).ToString(),
        });
    }

    /// <summary>The dimmer's state, or its first preset switched off if it was never set.</summary>
    private DimmerState DimmerStateOf(Habbo.Silo.Rooms.RoomItemStore.PlacedItem dimmer) =>
        DimmerState.Parse(dimmer.Data.State) is { } state && state.PresetId >= 1 && state.PresetId <= Dimmer.PresetCount
            ? state
            : new DimmerState(false, 1, Dimmer.DefaultEffectType, Dimmer.DefaultColor, Dimmer.DefaultBrightness);

    /// <summary>Every preset the dimmer keeps, the unsaved ones as they start out.</summary>
    private async Task<List<FurniDimmerPreset>> DimmerPresetsAsync(int itemId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var saved = await db.FurniDimmerPresets.AsNoTracking().Where(p => p.ItemId == itemId).ToDictionaryAsync(p => p.PresetId);
        return Enumerable.Range(1, Dimmer.PresetCount)
            .Select(id => saved.GetValueOrDefault(id) ?? new FurniDimmerPreset
            {
                ItemId = itemId, PresetId = id, EffectType = Dimmer.DefaultEffectType,
                ColorHex = Dimmer.DefaultColor, Brightness = Dimmer.DefaultBrightness,
            })
            .ToList();
    }

    public async Task SetBackgroundColorAsync(Guid playerId, int itemId, int hue, int saturation, int lightness)
    {
        if (!IsController(playerId) || _items?.Find(itemId) is not { Interaction: BackgroundColorInteraction } toner
            || hue is < 0 or > MaxToneValue || saturation is < 0 or > MaxToneValue || lightness is < 0 or > MaxToneValue)
            return;

        var values = BackgroundColorInteraction.Values(toner.Data);
        values[BackgroundColorInteraction.HueIndex] = hue;
        values[BackgroundColorInteraction.SaturationIndex] = saturation;
        values[BackgroundColorInteraction.LightnessIndex] = lightness;
        await _furniture!.UpdateAsync(toner, BackgroundColorInteraction.WithValues(toner.Data, values));
    }
}
