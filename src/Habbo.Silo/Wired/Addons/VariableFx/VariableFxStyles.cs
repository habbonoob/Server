namespace Habbo.Silo.Wired.Addons.VariableFx;

/// <summary>Every style of every category of variable display, as the client defines them (its style definitions, with
/// its colour, width and renderer ids). The client dereferences the style a config names without checking, so a config
/// only ever names one of these, with its colour, width and renderer resolved.</summary>
public static class VariableFxStyles
{
    public const int Health = 0;
    public const int Progress = 1;
    public const int Levelling = 2;
    public const int Status = 3;
    public const int Boss = 4;
    public const int Number = 5;

    // colours
    private const int NotApplicable = -1;
    private const int Green = 1;
    private const int Yellow = 3;
    private const int Red = 5;
    private const int Blue = 7;
    private const int White = 19;
    private const int RedToGreen = 1000;
    private const int LevellingColor = 1001;
    private const int TeamColor = 1002;

    private static readonly int[] NormalColors = [.. Enumerable.Range(1, 19), 100, 101, 102, 103, 104];
    private static readonly int[] AllAndTeam = [.. NormalColors, TeamColor];
    private static readonly int[] AllRedToGreenAndTeam = [.. NormalColors, RedToGreen, TeamColor];
    private static readonly int[] LevellingColors = [.. NormalColors, LevellingColor, TeamColor];
    private static readonly int[] NumberColors = [Red, Green, Blue, Yellow, White, TeamColor];

    // widths: extra small 0 to extra large 4, and the boss bar's 100
    private const int Medium = 2;
    private const int ExtraLarge = 4;
    private static readonly int[] SmallToLarge = [1, 2, 3];
    private static readonly int[] SmallToExtraLarge = [1, 2, 3, 4];
    private static readonly int[] ExtraSmallToExtraLarge = [0, 1, 2, 3, 4];
    private static readonly int[] LargeToHuge = [3, 4, 100];

    // renderers
    private const int BlockBar = 2;
    private static readonly int[] StatusRenderers = [2, 3, 4];

    public static readonly IReadOnlyList<VariableFxStyle> All =
    [
        new(Progress, 0, AllAndTeam, Green, [0], 0, ExtraSmallToExtraLarge, Medium, []),
        new(Progress, 1, AllAndTeam, Green, [2], 2, ExtraSmallToExtraLarge, Medium, []),
        new(Progress, 2, AllAndTeam, Green, [3], 3, ExtraSmallToExtraLarge, Medium, []),
        new(Progress, 3, AllAndTeam, Green, [4], 4, ExtraSmallToExtraLarge, Medium, []),
        new(Progress, 4, AllAndTeam, Green, [1], 1, ExtraSmallToExtraLarge, Medium, []),
        new(Health, 0, [RedToGreen], RedToGreen, [10], 10, SmallToLarge, Medium, [("icon", "misc_heart")]),
        new(Health, 3, AllAndTeam, Red, [11], 11, [NotApplicable], NotApplicable, []),
        new(Health, 1, AllAndTeam, Red, [12], 12, [2, 3], Medium, []),
        new(Health, 2, [NotApplicable], NotApplicable, [13], 13, ExtraSmallToExtraLarge, Medium, []),
        new(Levelling, 0, LevellingColors, LevellingColor, [20], 20, SmallToLarge, Medium, [("sub_renderer", "2")]),
        new(Levelling, 1, LevellingColors, LevellingColor, [21], 21, SmallToExtraLarge, Medium, [("sub_renderer", "1")]),
        new(Boss, 0, [Red], Red, [100], 100, LargeToHuge, ExtraLarge, [("icon_alignment", "double"), ("icon", "misc_skull")]),
        new(Boss, 1, AllRedToGreenAndTeam, Red, [100], 100, LargeToHuge, ExtraLarge, []),
        new(Number, 0, NumberColors, Green, [201], 201, [NotApplicable], NotApplicable, [("design", "freeze_style")]),
        new(Number, 1, AllAndTeam, Green, [200], 200, [NotApplicable], NotApplicable, [("design", "shalimar")]),
        new(Number, 2, AllAndTeam, Green, [200], 200, [NotApplicable], NotApplicable, [("design", "blocky")]),
        StatusBar(0, "energy", "#ffd83d"),
        StatusBar(1, "shield", "#4aa9f6"),
        StatusBar(2, "magic", "#8751d1"),
        StatusBar(3, "food", "#ff9f24"),
        StatusBar(4, "stamina", "#86d213"),
        StatusBar(5, "poison", "#8ddc35"),
        StatusBar(6, "mana", "#268fff"),
        StatusBar(7, "health", "#7dce35"),
        StatusBar(8, "gold", "#ffc83d", metallic: true),
        StatusBar(9, "gems", "#416bdd", metallic: true),
        StatusBar(10, "honor", "#fac384"),
        StatusBar(11, "reputation", "#ffd83d"),
        StatusBar(12, "cooldown", "#b8c3cc"),
        StatusBar(13, "timeleft", "#74b9e8"),
        StatusBar(14, "burning", "#ff5a1f"),
        StatusBar(15, "freezing", "#82cfff"),
        new(Status, 16, [RedToGreen], RedToGreen, StatusRenderers, BlockBar, ExtraSmallToExtraLarge, Medium, [("icon", "battery")]),
        StatusBar(17, "repairing", "#c9c5b8", metallic: true),
        StatusBar(18, "stealth", "#6254a8"),
        StatusBar(19, "upgrading", "#6bdc34"),
        StatusBar(20, "star_power", "#ffd900", metallic: true),
        StatusBar(21, "droplet", "#4aabf5"),
    ];

    /// <summary>The category's style, or its first when it has no such style, as the client's editor falls back.</summary>
    public static VariableFxStyle Of(int category, int styleId) =>
        All.FirstOrDefault(style => style.Category == category && style.StyleId == styleId)
        ?? All.First(style => style.Category == category);

    private static VariableFxStyle StatusBar(int styleId, string icon, string color, bool metallic = false) =>
        new(Status, styleId, [NotApplicable], NotApplicable, StatusRenderers, BlockBar, ExtraSmallToExtraLarge, Medium,
            [("color", color), ("icon", icon), ("metallic", metallic ? "true" : "false")]);
}
