using Xabbo;
using Xabbo.Core;
using Xabbo.Core.Messages.Incoming;
using Xabbo.Messages;

namespace Habbo.RoomCopier;

/// <summary>The room the client is in, kept from the packets it is sent: the floor plan, door, walls and floor, and
/// every furni as it is added, moved, changed and taken away. Each packet is read on its own, and one that can't be
/// read (Habbo changes a layout now and then) is reported and passed over, rather than stopping the copier.</summary>
public sealed class RoomCapture
{
    private readonly Dictionary<string, Action<IPacket>> _readers = [];
    private readonly Dictionary<long, IFloorItem> _floorItems = [];
    private readonly Dictionary<long, IWallItem> _wallItems = [];

    public long RoomId { get; private set; } = -1;
    public string Name { get; private set; } = "";
    public string Description { get; private set; } = "";
    public string? Heightmap { get; private set; }
    public bool UseSmallScale { get; private set; }
    public int FixedWallsHeight { get; private set; } = -1;
    public (int X, int Y, int Direction) Door { get; private set; }
    public bool WallsHidden { get; private set; }
    public int WallThickness { get; private set; }
    public int FloorThickness { get; private set; }
    public string Wallpaper { get; private set; } = "";
    public string Floor { get; private set; } = "";
    public string Landscape { get; private set; } = "";

    /// <summary>Whether the room lets the player read its wired boxes' settings (WiredPermissions); null until told.</summary>
    public bool? CanReadWired { get; private set; }

    /// <summary>The player's own id and, in the room, their avatar's index: for a whisper only they see.</summary>
    public long SelfId { get; private set; } = -1;
    public int? SelfIndex { get; private set; }

    /// <summary>Whether the room's furni came yet: the avatar sent alone after them is the one entering.</summary>
    private bool _furniCame;

    public bool InRoom => RoomId >= 0 && Heightmap is not null;
    public IReadOnlyCollection<IFloorItem> FloorItems => _floorItems.Values;
    public IReadOnlyCollection<IWallItem> WallItems => _wallItems.Values;

    public RoomCapture()
    {
        On("UserObject", p => SelfId = p.Read<int>());
        On("OpenConnection", p => Enter(p.Read<int>()));
        On("CloseConnection", _ => Enter(-1));
        On("GetGuestRoomResult", p =>
        {
            p.Read<bool>();
            var id = p.Read<int>();
            var name = p.Read<string>();
            p.Read<int>();
            p.Read<string>();
            p.Read<int>();
            p.Read<int>();
            p.Read<int>();
            var description = p.Read<string>();
            if (id == RoomId)
                (Name, Description) = (name, description);
        });
        On("FloorHeightmap", p =>
        {
            UseSmallScale = p.Read<bool>();
            FixedWallsHeight = p.Read<int>();
            Heightmap = p.Read<string>();
        });
        On("WiredPermissions", p =>
        {
            var canModify = p.Read<bool>();
            CanReadWired = canModify || p.Read<bool>();
        });
        On("RoomEntryTile", p => Door = (p.Read<int>(), p.Read<int>(), p.Read<int>()));
        On("RoomVisualizationSettings", p => (WallsHidden, WallThickness, FloorThickness) = (p.Read<bool>(), p.Read<int>(), p.Read<int>()));
        On("RoomProperty", p =>
        {
            var key = p.Read<string>();
            var value = p.Read<string>();
            switch (key)
            {
                case "wallpaper": Wallpaper = value; break;
                case "floor": Floor = value; break;
                case "landscape": Landscape = value; break;
            }
        });

        On("Objects", p =>
        {
            _furniCame = true;
            _floorItems.Clear();
            foreach (var item in p.Read<FloorItemsMsg>())
                _floorItems[item.Id] = item;
        });
        On("ObjectAdd", p => Keep(p.Read<FloorItemAddedMsg>().Item));
        On("ObjectUpdate", p => Keep(p.Read<FloorItemUpdatedMsg>().Item));
        On("ObjectRemove", p => _floorItems.Remove(p.Read<FloorItemRemovedMsg>().Id));
        On("ObjectDataUpdate", p =>
        {
            var update = p.Read<FloorItemDataUpdatedMsg>();
            if (_floorItems.TryGetValue(update.Id, out var item) && item is FloorItem floor)
                floor.Data = update.Data;
        });
        On("Items", p =>
        {
            _wallItems.Clear();
            foreach (var item in p.Read<WallItemsMsg>())
                _wallItems[item.Id] = item;
        });
        On("ItemAdd", p => Keep(p.Read<WallItemAddedMsg>().Item));
        On("ItemUpdate", p => Keep(p.Read<WallItemUpdatedMsg>().Item));
        On("ItemRemove", p => _wallItems.Remove(p.Read<WallItemRemovedMsg>().Id));
        On("ItemDataUpdate", p =>
        {
            var update = p.Read<ItemDataMsg>();
            if (_wallItems.TryGetValue(update.Id, out var item) && item is WallItem wall)
                wall.Data = update.Data;
        });

        On("Users", p =>
        {
            var avatars = p.Read<AvatarsAddedMsg>();
            foreach (var avatar in avatars)
                if (avatar.Id == SelfId)
                    SelfIndex = avatar.Index;
            // Habbo sends the one entering on their own, after the furniture (UserObject, which names them, comes
            // only at login)
            if (SelfIndex is null && _furniCame && avatars.Count == 1)
                (SelfIndex, SelfId) = (avatars[0].Index, avatars[0].Id);
        });
    }

    private void Enter(long roomId)
    {
        RoomId = roomId;
        (Name, Description, Heightmap, SelfIndex, _furniCame) = ("", "", null, null, false);
        (UseSmallScale, FixedWallsHeight, Door, WallsHidden, WallThickness, FloorThickness) = (false, -1, default, false, 0, 0);
        (Wallpaper, Floor, Landscape, CanReadWired) = ("", "", "", null);
        _floorItems.Clear();
        _wallItems.Clear();
    }

    private void Keep(IFloorItem item) => _floorItems[item.Id] = item;
    private void Keep(IWallItem item) => _wallItems[item.Id] = item;

    /// <summary>Takes in a packet the client was sent, by its name in the client; one that can't be read is reported
    /// and left, and the packet goes on to the client as it came.</summary>
    public void Read(string name, IPacket packet)
    {
        if (!_readers.TryGetValue(name, out var read))
            return;
        var position = packet.Position;
        try
        {
            read(packet);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not read {name}: {ex.Message}");
        }
        finally
        {
            packet.Position = position;
        }
    }

    private void On(string name, Action<IPacket> read) => _readers[name] = read;
}
