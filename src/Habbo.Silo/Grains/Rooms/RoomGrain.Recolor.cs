using Habbo.Protocol.Messages.Incoming.Room.Furniture;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Silo.Rooms;
using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>The recolour window of recolourable furni (Builders Club's bc_*_recolorable): one with rights, as for moving
/// furni, gives a furni a colour of its palette — alone, or with the room's furni of its colour, its type, or both. The
/// colour is kept in each furni's map data (r, g, b), which wired's ~recolorable_furni variables read and write too.</summary>
public sealed partial class RoomGrain
{
    public async Task RecolorFurniAsync(Guid playerId, int itemId, int scope, int red, int green, int blue)
    {
        if (!IsController(playerId) || _items?.Find(itemId) is not { } furni
            || WiredInternalVariableAccess.ColorOf(furni) is not { } was
            || red is < 0 or > byte.MaxValue || green is < 0 or > byte.MaxValue || blue is < 0 or > byte.MaxValue)
            return;

        var color = red << 16 | green << 8 | blue;
        var className = furni.Item.Definition!.ClassName;
        bool SameType(RoomItemStore.PlacedItem item) => item.Item.Definition!.ClassName == className;
        bool SameColor(RoomItemStore.PlacedItem item) => WiredInternalVariableAccess.ColorOf(item) == was;
        IEnumerable<RoomItemStore.PlacedItem> chosen = scope switch
        {
            FurniRecolorMessageEvent.OnlyThis => [furni],
            FurniRecolorMessageEvent.SameColor => _items.Items.Where(SameColor),
            FurniRecolorMessageEvent.SameType => _items.Items.Where(item => SameType(item) && WiredInternalVariableAccess.ColorOf(item) is not null),
            FurniRecolorMessageEvent.SameTypeAndColor => _items.Items.Where(item => SameType(item) && SameColor(item)),
            _ => [],
        };

        // only to a colour each furni's own palette offers, as the window does
        var palettes = new Dictionary<string, IReadOnlySet<int>>();
        var recolored = new List<RoomItemStore.PlacedItem>();
        foreach (var item in chosen.ToList())
        {
            var name = item.Item.Definition!.ClassName;
            if (!palettes.TryGetValue(name, out var palette))
                palettes[name] = palette = await RecolorPalettes.ForAsync(clientVariables, name);
            if (palette.Contains(color))
                recolored.Add(item);
        }

        if (!recolored.Contains(furni))
            return;

        foreach (var item in recolored)
            await _furniture!.SaveAsync(item, WiredInternalVariableAccess.WithColor(item.Data, red, green, blue));
        Broadcast(new FurniRecolorUpdateMessageComposer(
            [new RecolorUpdateData(recolored.Select(item => item.Id).ToList(), (short)red, (short)green, (short)blue)]));
    }
}
