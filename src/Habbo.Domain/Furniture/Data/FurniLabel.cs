namespace Habbo.Domain.Furniture.Data;

/// <summary>A wired label's note (wf_label): what it says, the colour of its bubble, who sees it and whether it stays
/// open. A label not yet written has no row and is shown as <see cref="Default"/>.</summary>
public sealed class FurniLabel : IFurniRow
{
    public const int MaxTextLength = 512;

    /// <summary>Who sees the label: everyone in the room, or only those who may read the room's wired.</summary>
    public const int VisibleToEveryone = 0;
    public const int VisibleToWiredReaders = 1;

    /// <summary>The bubble colours the label's settings offer (0xRRGGBB), the first the client's own default.</summary>
    public static readonly IReadOnlyList<int> BubbleColors = [0x414A80, 0x28766F, 0x3F7044, 0x806D32, 0x80513A, 0x803E4A, 0x684780, 0x555555];

    public static FurniLabel Default(int itemId) => new() { ItemId = itemId };

    public int ItemId { get; set; }
    public UserItem? Item { get; set; }

    public string Text { get; set; } = string.Empty;
    public int BubbleColor { get; set; } = BubbleColors[0];
    public int Visibility { get; set; } = VisibleToWiredReaders;

    /// <summary>Open without being clicked; only a label everyone sees may be.</summary>
    public bool AlwaysOpen { get; set; }
}
