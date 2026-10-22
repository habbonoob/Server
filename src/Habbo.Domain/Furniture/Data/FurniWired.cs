namespace Habbo.Domain.Furniture.Data;

/// <summary>A wired box's settings, exactly as its dialog last saved them. The meaning of each
/// field is the box's own — a trigger's first int may be a period, an effect's a direction — so
/// they are kept as the client sends them and read by the box that owns them.</summary>
public sealed class FurniWired : IFurniRow
{
    /// <summary>The longest text a box keeps: a message to show, a keyword to listen for. The
    /// dialogs allow a thousand characters.</summary>
    public const int StringParamMaxLength = 1024;

    public int ItemId { get; set; }
    public UserItem? Item { get; set; }

    public List<int> IntParams { get; set; } = [];
    public string StringParam { get; set; } = string.Empty;

    /// <summary>The furni picked in the dialog, by item id.</summary>
    public List<int> StuffIds { get; set; } = [];

    /// <summary>The second pick, for boxes that take two sets (move furni to furni, say).</summary>
    public List<int> StuffIds2 { get; set; } = [];

    /// <summary>Where each furni target slot takes its furni from, one source per slot.</summary>
    public List<int> FurniSources { get; set; } = [];

    /// <summary>Where each user target slot takes its users from, one source per slot.</summary>
    public List<int> UserSources { get; set; } = [];

    public List<string> VariableIds { get; set; } = [];

    /// <summary>Effects: how many pulses of half a second to wait before running.</summary>
    public int DelayInPulses { get; set; }

    /// <summary>Conditions: whether all or any of the targets must pass.</summary>
    public int Quantifier { get; set; }

    /// <summary>Selectors: narrow what earlier selectors chose, rather than choose afresh.</summary>
    public bool IsFilter { get; set; }

    /// <summary>Selectors: choose everything the selector would not.</summary>
    public bool IsInvert { get; set; }

    /// <summary>The picked furni as they stood when the box was saved, for the boxes that match
    /// or restore that picture.</summary>
    public List<FurniWiredSnapshot> Snapshots { get; set; } = [];
}
