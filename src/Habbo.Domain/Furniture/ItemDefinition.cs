namespace Habbo.Domain.Furniture;

public enum ItemType
{
    Floor,
    Wall,
}

/// <summary>A furniture definition. The fields mirror furnidata, which is the authority: the
/// client draws from its own copy, so anything we disagree with it about shows up as furniture
/// that behaves differently than it looks.
///
/// furnidata numbers floor and wall items separately, so a class id alone is ambiguous —
/// 4001 is both the "nest_dragon" floor item and the "poster" wall item. The client never
/// notices because the wire always carries the type next to the id. Rows therefore get their
/// own surrogate <see cref="Id"/>, and <see cref="ClassId"/> with <see cref="Type"/> is what
/// identifies an item to the client.</summary>
public sealed class ItemDefinition
{
    /// <summary>Our own key. Never sent to the client.</summary>
    public int Id { get; set; }

    /// <summary>The furnidata id, unique only within <see cref="Type"/>. This is the id the
    /// client resolves against its own furnidata, so this is what goes on the wire.</summary>
    public int ClassId { get; set; }

    public required string ClassName { get; set; }
    public ItemType Type { get; set; } = ItemType.Floor;
    public int XDim { get; set; } = 1;
    public int YDim { get; set; } = 1;
    public double StackHeight { get; set; } = 1.0;
    public bool CanWalk { get; set; }
    public bool CanSit { get; set; }
    public bool CanLay { get; set; }

    /// <summary>Whether other furniture may be stacked on top of this one.</summary>
    public bool CanStackOn { get; set; }

    /// <summary>furnidata's own category name, e.g. "chair" or "wall_decoration".</summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Whether copies of this item stack into one inventory tile with a count.
    /// The client keys a stack on type alone, so anything that carries state worth telling
    /// apart — a trophy with a name on it, a limited edition — has to opt out here.
    /// furnidata does not express this, so it stays true unless something sets it.</summary>
    public bool Groupable { get; set; } = true;

    /// <summary>The display name and blurb the client shows; furnidata leaves both empty for
    /// some items, which is why neither is required.</summary>
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>The asset revision — the folder the client loads this item's SWF from.</summary>
    public int Revision { get; set; }

    /// <summary>The rotation the item is placed at when no direction is given.</summary>
    public int DefaultDirection { get; set; }

    /// <summary>furnidata's specialtype: 1 is ordinary furniture, higher values mark the
    /// behaviours the client special-cases (posters, gates, and so on).</summary>
    public int SpecialType { get; set; } = 1;

    /// <summary>The themed range this item belongs to, e.g. "area" or "iced".</summary>
    public string FurniLine { get; set; } = string.Empty;

    public bool Rare { get; set; }
    public bool Tradeable { get; set; } = true;
    public bool Recyclable { get; set; } = true;

    /// <summary>furnidata's customparams: configuration for the item's behaviour, in a format
    /// that behaviour defines — an effect id for an effect box, "states=8,delay=2000" for a
    /// fortune wheel. Only the behaviour reading it knows what it means, so it is kept as given.</summary>
    public string CustomParams { get; set; } = string.Empty;

    /// <summary>The client's name for the code that runs this item, e.g. "furniture_multistate".
    /// It comes from the item's own SWF, not furnidata, and decides what the client does with
    /// a click and which layout it expects the item's data in.</summary>
    public string Logic { get; set; } = BasicLogic;

    /// <summary>The logic every item without a more specific one runs: nothing happens on use.</summary>
    public const string BasicLogic = "furniture_basic";

    /// <summary>What this item is worth when exchanged for credits, for the furniture that can
    /// be. It comes from the item's SWF, which is the only place it is written down; furnidata
    /// says nothing about it. Null for everything that is not credit furniture.</summary>
    public int? CreditValue { get; set; }

    /// <summary>The client's name for how the item is drawn, e.g. "furniture_animated".</summary>
    public string Visualization { get; set; } = string.Empty;

    /// <summary>The visualization that draws a water surface with a shore edge around it.</summary>
    public const string WaterAreaVisualization = "furniture_water_area";

    /// <summary>True for furniture the client draws as a water area. Its state is not the item's
    /// own: the room works out where the water meets land and sends that as a border mask.</summary>
    public bool IsWaterArea => Visualization == WaterAreaVisualization;

    /// <summary>The server behaviour, when it differs from what <see cref="Logic"/> implies.
    /// The client runs a wired box as a plain multistate item, for instance, but the server has
    /// to know it is wired. Null means the logic says everything.</summary>
    public string? Interaction { get; set; }

    /// <summary>What a dispenser hands out, one at random per use: hand items for a vending machine, avatar effects for
    /// an effect dispenser. From seed/vending.json, as furnidata no longer says (its custom params, where it still
    /// has them, are the fallback). Empty for everything that dispenses nothing.</summary>
    public List<int> VendingIds { get; set; } = [];

    /// <summary>The name the server looks the item's behaviour up by.</summary>
    public string InteractionName => Interaction ?? Logic;

    /// <summary>The visual states the item's SWF can draw. Always includes 0, the resting look,
    /// which the SWF draws without an animation of its own.</summary>
    public List<ItemDefinitionState> States { get; set; } = [];

    /// <summary>The rotations the item's SWF has artwork for, as wire directions (0–7). Empty
    /// when the SWF was never read, in which case the four square rotations are let through.</summary>
    public List<ItemDefinitionDirection> Directions { get; set; } = [];

    public bool AllowsDirection(int direction) =>
        direction is >= 0 and <= 7
        && (Directions.Count == 0 ? direction % 2 == 0 : Directions.Any(d => d.Direction == direction));

    /// <summary>The wire sizeZ: the item's own height, which the client uses as the furniture's
    /// height for stacking and seating. Habbo sends the furnidata height verbatim — a packet
    /// capture of a water tile (height 0) carries "0.0" — so nothing is added to it here.</summary>
    public double SizeZ => StackHeight;
}
