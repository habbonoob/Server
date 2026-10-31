using Habbo.Domain.Furniture.Data;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;
using Habbo.Silo.Wired;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Wired labels (wf_label): notes on the floor that those who may change the room's wired write and set up.
/// Everyone in the room is told every label, on entry and as one changes; the client shows each only to those its
/// visibility allows.</summary>
public sealed partial class RoomGrain
{
    private const string LabelTextRejected = "wiredfurni.error.label.unacceptable_text";
    private const string LabelSettingsInvalid = "wiredfurni.error.label.invalid_settings";

    /// <summary>The notes of the room's labels that have been written or set up, by item (its database id, as they are
    /// saved); a label without one shows <see cref="FurniLabel.Default"/>. The client knows a label by the id the
    /// room knows it by.</summary>
    private readonly Dictionary<int, FurniLabel> _labels = [];

    private static bool IsLabel(RoomItemStore.PlacedItem item) => item.Interaction is WiredLabelInteraction;

    private async Task LoadLabelsAsync(AppDbContext db, CancellationToken ct)
    {
        var ids = _items!.Items.Where(IsLabel).Select(item => item.Item.Id).ToList();
        if (ids.Count == 0)
            return;

        foreach (var label in await db.FurniLabels.AsNoTracking().Where(l => ids.Contains(l.ItemId)).ToListAsync(ct))
            _labels[label.ItemId] = label;
    }

    /// <summary>Every label in the room, as the entry sequence shows them.</summary>
    private List<RoomLabelData> Labels() =>
        _items!.Items.Where(IsLabel).Select(item => ToLabelData(item, LabelOf(item))).ToList();

    private FurniLabel LabelOf(RoomItemStore.PlacedItem item) =>
        _labels.GetValueOrDefault(item.Item.Id) ?? FurniLabel.Default(item.Item.Id);

    private static RoomLabelData ToLabelData(RoomItemStore.PlacedItem item, FurniLabel label) =>
        new(item.Id, label.Text, label.BubbleColor, label.Visibility, label.AlwaysOpen);

    /// <summary>A label put down shows its note, the one it had before it was picked up.</summary>
    private async Task LabelPlacedAsync(RoomItemStore.PlacedItem placed)
    {
        if (!IsLabel(placed))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.FurniLabels.AsNoTracking().FirstOrDefaultAsync(l => l.ItemId == placed.Item.Id) is { } label)
            _labels[label.ItemId] = label;
        BroadcastLabel(placed, LabelOf(placed));
    }

    public async Task SetFurnitureLabelAsync(Guid playerId, int itemId, string text)
    {
        if (LabelWriter(playerId, itemId) is not { } item)
            return;

        if (text.Length > FurniLabel.MaxTextLength || (await wordFilter.FilterAsync(text, _filterWords)).FirstHit is not null)
        {
            SendTo(playerId, WiredDialogs.Refusal(WiredValidation.Of(LabelTextRejected)));
            return;
        }

        await SaveLabelAsync(item, label => label.Text = text);
    }

    public async Task SetFurnitureLabelSettingsAsync(Guid playerId, int itemId, int bubbleColor, int visibility, bool alwaysOpen)
    {
        if (LabelWriter(playerId, itemId) is not { } item)
            return;

        // only the colours the settings offer, and open by itself only to everyone
        if (!FurniLabel.BubbleColors.Contains(bubbleColor)
            || visibility is not (FurniLabel.VisibleToEveryone or FurniLabel.VisibleToWiredReaders)
            || (alwaysOpen && visibility != FurniLabel.VisibleToEveryone))
        {
            SendTo(playerId, WiredDialogs.Refusal(WiredValidation.Of(LabelSettingsInvalid)));
            return;
        }

        await SaveLabelAsync(item, label =>
        {
            label.BubbleColor = bubbleColor;
            label.Visibility = visibility;
            label.AlwaysOpen = alwaysOpen;
        });
    }

    /// <summary>The label, when the user is here and may change the room's wired.</summary>
    private RoomItemStore.PlacedItem? LabelWriter(Guid playerId, int itemId) =>
        _entities?.Find(playerId) is { } entity && WiredRights(entity).Modify
        && _items?.Find(itemId) is { } item && IsLabel(item) ? item : null;

    private async Task SaveLabelAsync(RoomItemStore.PlacedItem item, Action<FurniLabel> change)
    {
        var itemId = item.Item.Id;
        await using var db = await dbFactory.CreateDbContextAsync();
        var label = await db.FurniLabels.FirstOrDefaultAsync(l => l.ItemId == itemId);
        if (label is null)
        {
            label = FurniLabel.Default(itemId);
            db.FurniLabels.Add(label);
        }

        change(label);
        await db.SaveChangesAsync();
        label.Item = null;
        _labels[itemId] = label;
        BroadcastLabel(item, label);
    }

    private void BroadcastLabel(RoomItemStore.PlacedItem item, FurniLabel label) =>
        Broadcast(new FurnitureLabelsAndRegionsUpdateComposer([ToWire(ToLabelData(item, label))], []));

    private static FurnitureLabelData ToWire(RoomLabelData label) =>
        new(FurnitureLabelData.LabelType, label.ItemId, label.Text, label.BubbleColor, label.Visibility, label.AlwaysOpen);
}
