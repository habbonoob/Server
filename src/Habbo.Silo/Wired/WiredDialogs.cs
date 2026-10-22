using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;
using Habbo.Silo.Abstractions.Wired;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired;

/// <summary>Builds the message that opens a wired box's dialog, filled in with its saved settings.</summary>
public static class WiredDialogs
{
    /// <summary>Dialogs open with the advanced section closed; the player opens it for the sources.</summary>
    private const bool AdvancedSectionOpen = false;

    public static IComposer For(WiredBox box, RoomItemStore items, IReadOnlyList<WiredContextEntryData> contextEntries)
    {
        var type = box.Type;
        var settings = box.Settings;

        // where each of the box's furni and user slots may take its targets from
        var furniSlots = type.FurniSlots.Select(slot => new WiredSourceChoiceData(slot.Sources)).ToList();
        var userSlots = type.UserSlots.Select(slot => new WiredSourceChoiceData(slot.Sources)).ToList();
        var defaultFurni = type.FurniSlots.Select(slot => slot.Default).ToList();
        var defaultUsers = type.UserSlots.Select(slot => slot.Default).ToList();

        var data = new WiredBoxData(
            // Habbo sends the limit to every box, picking or not
            FurniLimit: WiredBoxType.MaxPickedFurni,
            // picks that have since left the room are not shown
            StuffIds: settings.StuffIds.Where(id => items.Find(id) is not null).ToList(),
            StuffIds2: settings.StuffIds2.Where(id => items.Find(id) is not null).ToList(),
            StuffTypeId: box.Item.Item.Definition!.ClassId,
            ItemId: box.Id,
            StringParam: settings.StringParam,
            IntParams: settings.IntParams.Count > 0 ? settings.IntParams : type.DefaultIntParams,
            VariableIds: settings.VariableIds,
            FurniSources: settings.FurniSources.Count == furniSlots.Count ? settings.FurniSources : defaultFurni,
            UserSources: settings.UserSources.Count == userSlots.Count ? settings.UserSources : defaultUsers,
            Code: type.Code);
        var options = new WiredBoxOptionsData(AdvancedSectionOpen, furniSlots, userSlots, defaultFurni, defaultUsers, type.AllowsWallFurni);
        var context = new WiredBoxContextData(contextEntries, type.ResetIntParams);

        return type.Kind switch
        {
            WiredKind.Trigger => new WiredFurniTriggerComposer(data, options, context),
            WiredKind.Effect => new WiredFurniActionComposer(data, settings.DelayInPulses, options, context),
            WiredKind.Condition when type is WiredCondition condition => new WiredFurniConditionComposer(
                data, settings.Quantifier, options, condition.QuantifierType, condition.InvertedInDialog, context),
            WiredKind.Selector => new WiredFurniSelectorComposer(data, settings.IsFilter, settings.IsInvert, options, context),
            WiredKind.Addon => new WiredFurniAddonComposer(data, options, context),
            _ => new WiredFurniVariableComposer(data, options, context),
        };
    }

    public static IComposer Refusal(WiredValidation validation) =>
        new WiredValidationErrorComposer(
            validation.LocalizationKey,
            validation.Parameters.Select(p => new WiredValidationParameterData(p.Key, p.Value)).ToList());
}
