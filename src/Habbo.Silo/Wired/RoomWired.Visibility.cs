using Habbo.Domain.Furniture;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired;

/// <summary>Furni and users made see-through by wired's set visibility. Shown to everyone, it stays on the furni or
/// user for the room's life (an invisible furni holds @is_invisible) and is shown to whoever comes in; shown to an
/// audience, it is only told to them.</summary>
public sealed partial class RoomWired
{
    public void SetTransparency(IReadOnlyList<RoomItemStore.PlacedItem> furni, IReadOnlyList<RoomEntityStore.RoomEntity> users,
        WiredTransparency transparency, IReadOnlyList<RoomEntityStore.RoomEntity>? audience)
    {
        var (shown, allowClickThrough, easing) = (transparency.Transparency, transparency.AllowClickThrough, transparency.EasingMode);
        var furniMessage = furni.Count == 0 ? null
            : new WiredFurniTransparencyComposer(furni.Select(TransparencyId).ToList(), shown, allowClickThrough, easing);
        var userMessage = users.Count == 0 ? null
            : new WiredUserTransparencyComposer(users.Select(user => user.RoomIndex).ToList(), shown, allowClickThrough, easing);

        if (audience is null)
        {
            var kept = transparency.IsDefault ? null : transparency;
            foreach (var item in furni)
                item.Transparency = kept;
            foreach (var user in users)
                user.Transparency = kept;
            if (furniMessage is not null)
                broadcast(furniMessage);
            if (userMessage is not null)
                broadcast(userMessage);
            return;
        }

        foreach (var viewer in audience.Where(viewer => viewer is { Bot: null, Pet: null } && IsPresent(viewer)).Distinct())
        {
            if (furniMessage is not null)
                sendTo(viewer.Player.Id, furniMessage);
            if (userMessage is not null)
                sendTo(viewer.Player.Id, userMessage);
        }
    }

    /// <summary>Someone came in: the furni and users wired left see-through for everyone, as they stand.</summary>
    private void WelcomeToVisibility(RoomEntityStore.RoomEntity user)
    {
        if (user.Bot is not null || user.Pet is not null)
            return;

        var furni = items.Items.Where(item => item.Transparency is not null)
            .Select(item => TransparencyData(TransparencyId(item), item.Transparency!))
            .ToList();
        var users = entities.Entities.Where(entity => entity.Transparency is not null)
            .Select(entity => TransparencyData(entity.RoomIndex, entity.Transparency!))
            .ToList();
        if (furni.Count > 0)
            sendTo(user.Player.Id, new WiredFurniVisualizationStateComposer(furni, []));
        if (users.Count > 0)
            sendTo(user.Player.Id, new WiredUserVisualizationStateComposer(users, []));
    }

    /// <summary>How the client knows a furni here: a floor item by its id, a wall item by its id negated.</summary>
    private static int TransparencyId(RoomItemStore.PlacedItem item) =>
        item.Item.Definition!.Type == ItemType.Wall ? -item.Id : item.Id;

    private static WiredTransparencyData TransparencyData(int objectId, WiredTransparency transparency) =>
        new(objectId, transparency.Transparency, transparency.AllowClickThrough, transparency.EasingMode);
}
