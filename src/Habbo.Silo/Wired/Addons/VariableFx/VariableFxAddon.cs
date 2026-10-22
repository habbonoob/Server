using System.Globalization;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.VariableFx;
using Habbo.Silo.Rooms;
using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Addons.VariableFx;

/// <summary>A variable display (September 2026): stacked with a user or furni variable's box, it shows the variable's
/// value over its holders, as a health bar, a progress bar, a level, a status, a boss bar or a number. The room sends
/// its look as a config (the add-on's item id is its id) and each holder's value as a status, to the audience the
/// dialog chose (Habbo: experiment.fx, fx_style_updates). The settings are cleaned as the client's editor cleans them.
/// <para>Fields, in the dialog's order: source (0 furni, 1 users), audience, show mode, change mask, show on hover,
/// duration, style, colour, width, renderer, default min and max (each a long as two ints), override min and max on,
/// their targets, the audience variable's value (a long), segments; and for some a 22nd. Variables: override min,
/// override max, audience.</para></summary>
public abstract class VariableFxAddon : WiredAddon
{
    private const int SourceType = 0;
    private const int Audience = 1;
    private const int ShowMode = 2;
    private const int UpdateMask = 3;
    private const int ShowOnHover = 4;
    private const int Duration = 5;
    private const int Style = 6;
    private const int Color = 7;
    private const int Width = 8;
    private const int Renderer = 9;
    private const int DefaultMin = 10;
    private const int DefaultMax = 12;
    private const int OverrideMinOn = 14;
    private const int OverrideMaxOn = 15;
    private const int OverrideMinTarget = 16;
    private const int OverrideMaxTarget = 17;
    private const int AudienceValue = 18;
    private const int Segments = 20;
    protected const int Extra = 21;

    private const int OverrideMinVariable = 0;
    private const int OverrideMaxVariable = 1;
    private const int AudienceVariable = 2;

    private const int FurniSource = 0;
    private const int UserSource = 1;

    // audience
    private const int OnlyTheUser = 0;
    private const int SameTeam = 1;
    private const int Everyone = 2;
    private const int HoldingVariable = 3;
    private const int VariableEquals = 4;

    private const int MaxShowMode = 2;
    private const int MaxUpdateMask = 15;
    private const int MinDuration = 1500;
    private const int MaxDuration = 20000;
    private const int MaxSegments = 100;
    private const int DefaultRangeMax = 100;

    /// <summary>The renderers drawn in segments.</summary>
    private static readonly int[] SegmentedRenderers = [2, 4, 13];

    /// <summary>A new display on Habbo: over users, to everyone, always shown, every change, three seconds, the first style
    /// with its own colour, width and renderer, 0 to 100.</summary>
    public override IReadOnlyList<int> DefaultIntParams => [1, 2, 0, 7, 0, 3000, 0, -1, -1, -1, 0, 0, 0, 100, 0, 0, 1, 1, 0, 0, 0];

    /// <summary>The display's category (<see cref="VariableFxStyles"/>).</summary>
    public abstract int Category { get; }

    /// <summary>The extras the display's own settings add to its style's: its segments, and whatever its category adds.</summary>
    protected virtual IEnumerable<(string Key, string Value)> OwnExtras(WiredBox box, VariableFxStyle style, int renderer)
    {
        if (box.IntParam(Segments) is > 0 and var segments && SegmentedRenderers.Contains(renderer))
            yield return ("segments", Math.Min(segments, MaxSegments).ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>The range the config sends; the levelling display sends none (-1, -1), its statuses carry each level's.</summary>
    protected virtual (long Min, long Max) Range(WiredBox box)
    {
        var (min, max) = (Long(box, DefaultMin), Long(box, DefaultMax));
        return max > min ? (min, max) : (min, min >= DefaultRangeMax ? min + 1 : DefaultRangeMax);
    }

    public static bool ForUsers(WiredBox box) => box.IntParam(SourceType) != FurniSource;

    public VariableFxConfigData Config(WiredBox box)
    {
        var style = VariableFxStyles.Of(Category, box.IntParam(Style));
        var color = style.Colors.Contains(box.IntParam(Color)) ? box.IntParam(Color) : style.DefaultColor;
        var width = style.Widths.Contains(box.IntParam(Width)) ? box.IntParam(Width) : style.DefaultWidth;
        var renderer = style.Renderers.Contains(box.IntParam(Renderer)) ? box.IntParam(Renderer) : style.DefaultRenderer;
        var showMode = box.IntParam(ShowMode) is >= 0 and <= MaxShowMode and var mode ? mode : 0;
        var (min, max) = Range(box);
        var extras = OwnExtras(box, style, renderer).Concat(style.Extras.Where(extra => extra.Key != "sub_renderer" || Category != VariableFxStyles.Levelling))
            .Select(extra => new VariableFxExtraData(extra.Key, extra.Value)).ToList();
        return new VariableFxConfigData(box.Id, ForUsers(box), showMode, Math.Clamp(box.IntParam(UpdateMask), 0, MaxUpdateMask),
            box.Flag(ShowOnHover), Math.Clamp(box.IntParam(Duration), MinDuration, MaxDuration), Category, style.StyleId, color, width, renderer,
            min, max, extras);
    }

    /// <summary>The display's value for one holder of its variable.</summary>
    public VariableFxStatusData Status(WiredBox box, WiredVariable variable, WiredHolder holder, int value, bool initialize, IWiredRoom room)
    {
        var (range, extras) = Overrides(box, variable, holder, value, room);
        return new VariableFxStatusData(Key(box, variable), initialize, holder.User is not null, EntityId(holder), value, range,
            extras.Select(extra => new VariableFxExtraData(extra.Key, extra.Value)).ToList());
    }

    /// <summary>A range of the holder's own, and extras, when the display has them: from the override variables; the
    /// levelling display from the variable's level system.</summary>
    protected virtual (VariableFxRangeData? Range, IReadOnlyList<(string Key, string Value)> Extras) Overrides(
        WiredBox box, WiredVariable variable, WiredHolder holder, int value, IWiredRoom room)
    {
        if (!box.Flag(OverrideMinOn) && !box.Flag(OverrideMaxOn))
            return (null, []);

        var (min, max) = Range(box);
        if (box.Flag(OverrideMinOn) && OverrideValue(box, OverrideMinVariable, OverrideMinTarget, holder, room) is { } overriddenMin)
            min = overriddenMin;
        if (box.Flag(OverrideMaxOn) && OverrideValue(box, OverrideMaxVariable, OverrideMaxTarget, holder, room) is { } overriddenMax)
            max = overriddenMax;
        return (new VariableFxRangeData(min, max), []);
    }

    private static int? OverrideValue(WiredBox box, int variableIndex, int targetIndex, WiredHolder holder, IWiredRoom room) =>
        WiredVariableUse.Variable(box, room, variableIndex) is { } variable
            ? room.VariableValue(variable, box.IntParam(targetIndex) == WiredVariableTargets.Global ? WiredHolder.Room : holder, null)
            : null;

    /// <summary>Who sees the display over the holder: as the dialog chose; a furni display only by everyone or by a
    /// variable, as the client's editor allows.</summary>
    public IEnumerable<RoomEntityStore.RoomEntity> Viewers(WiredBox box, WiredHolder holder, IWiredRoom room)
    {
        var users = room.Entities.Entities.Where(user => user.Bot is null && user.Pet is null);
        var audience = box.IntParam(Audience) is >= OnlyTheUser and <= VariableEquals and var chosen ? chosen : Everyone;
        if (!ForUsers(box) && audience is not (HoldingVariable or VariableEquals))
            audience = Everyone;

        var audienceVariable = WiredVariableUse.Variable(box, room, AudienceVariable);
        var wanted = Long(box, AudienceValue);
        return audience switch
        {
            OnlyTheUser => holder.User is { } only ? [only] : [],
            SameTeam => holder.User is { Team: not Rooms.RoomGames.NoTeam } member
                ? users.Where(user => user.Team == member.Team)
                : holder.User is { } alone ? [alone] : [],
            HoldingVariable => audienceVariable is null ? [] : users.Where(user => room.HasVariable(audienceVariable, WiredHolder.Of(user), null)),
            VariableEquals => audienceVariable is null ? [] : users.Where(user => room.VariableValue(audienceVariable, WiredHolder.Of(user), null) == wanted),
            _ => users,
        };
    }

    public static string Key(WiredBox box, WiredVariable variable) =>
        string.Create(CultureInfo.InvariantCulture, $"{box.Id}|{variable.Id}");

    /// <summary>The key a holder's value goes by when it is taken away.</summary>
    public static string RemovedKey(WiredBox box, WiredVariable variable, WiredHolder holder) =>
        string.Create(CultureInfo.InvariantCulture, $"{Key(box, variable)}|{(holder.User is null ? 'f' : 'u')}|{EntityId(holder)}");

    private static int EntityId(WiredHolder holder) => holder.User?.RoomIndex ?? holder.Item?.Item.Id ?? 0;

    /// <summary>A long the dialog sends as two ints: the high word, then the low.</summary>
    protected static long Long(WiredBox box, int highIndex) =>
        ((long)box.IntParam(highIndex) << 32) | (uint)box.IntParam(highIndex + 1);
}
