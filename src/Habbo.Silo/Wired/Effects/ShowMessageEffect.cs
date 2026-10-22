namespace Habbo.Silo.Wired.Effects;

/// <summary>Shows the box's text in the wired bubble, to its users or to everyone in the room.</summary>
[WiredBox("wf_act_show_message")]
public sealed class ShowMessageEffect : WiredEffect
{
    private const int Visibility = 0;
    private const int ToEveryone = 1;
    private const int Style = 1;

    /// <summary>The wired bubble, the dialog's default.</summary>
    public const int WiredBubbleStyle = 34;

    /// <summary>The chat styles the dialog offers (its NOTIFICATION_STYLES: the coloured, alert and
    /// symbol bubbles the client's chat styles define).</summary>
    public static readonly IReadOnlySet<int> Styles = new HashSet<int>
        { WiredBubbleStyle, 200, 201, 202, 210, 211, 212, 220, 221, 222, 223, 224, 225, 226, 227, 228, 229, 250, 251, 252 };

    public override int Code => EffectCodes.Chat;
    public override bool TargetsUsers => true;
    public override IReadOnlyList<int> DefaultIntParams => [0, WiredBubbleStyle, -1];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override Task RunAsync(WiredBox box, WiredRun run)
    {
        if (string.IsNullOrWhiteSpace(box.Settings.StringParam))
            return Task.CompletedTask;

        // to everyone once, in the voice of the first of its users; otherwise to each of them
        var users = run.Users(box).ToList();
        var toEveryone = box.IntParam(Visibility) == ToEveryone;
        var style = Styles.Contains(box.IntParam(Style, WiredBubbleStyle)) ? box.IntParam(Style, WiredBubbleStyle) : WiredBubbleStyle;
        foreach (var user in toEveryone ? users.Take(1) : users)
            run.Room.ShowMessage(user, run.Fill(box.Settings.StringParam), style, toEveryone);

        return Task.CompletedTask;
    }
}
