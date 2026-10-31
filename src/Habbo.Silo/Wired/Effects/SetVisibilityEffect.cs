using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Effects;

/// <summary>Makes its furni, or its users, invisible or visible again (the dialog's SET_FURNI_TRANSPARENCY): to its
/// audience users, or to everyone. Shown to everyone, it lasts for the room's life and an invisible furni holds
/// @is_invisible. Clicks may pass through what it hid, and the change eases in one of five ways; the client does both.
/// Only what is seen changes: a hidden furni still stands where it stood.</summary>
[WiredBox("wf_act_set_visibility")]
public sealed class SetVisibilityEffect : WiredEffect
{
    private const int ToEveryone = 0;
    private const int Transparency = 1;
    private const int AllowClickThrough = 2;
    private const int Easing = 3;
    private const int TargetsUsersFlag = 4;

    /// <summary>The dialog's easings, 0 to 4.</summary>
    private const int MaxEasing = 4;

    // its target is its furni or (the dialog's merged selections) its second user slot; the first is the audience
    private const int Targets = 0;
    private const int Audience = 0;
    private const int TargetUsers = 1;

    public override int Code => EffectCodes.SetFurniTransparency;
    public override bool AllowsWallFurni => true;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.Furni];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Users, WiredSlot.Users];

    public override Task RunAsync(WiredBox box, WiredRun run)
    {
        // the dialog offers visible and invisible only, and reads anything else as visible
        var transparency = new WiredTransparency(
            box.IntParam(Transparency) == WiredTransparency.Invisible ? WiredTransparency.Invisible : WiredTransparency.Visible,
            box.Flag(AllowClickThrough),
            box.IntParam(Easing) is var easing and >= 0 and <= MaxEasing ? easing : 0);
        var targetsUsers = box.Flag(TargetsUsersFlag);
        var furni = targetsUsers ? [] : run.Furni(box, Targets).ToList();
        var users = targetsUsers ? run.Users(box, TargetUsers).ToList() : [];
        if (furni.Count == 0 && users.Count == 0)
            return Task.CompletedTask;

        run.Room.SetTransparency(furni, users, transparency, box.Flag(ToEveryone) ? null : run.Users(box, Audience).ToList());
        return Task.CompletedTask;
    }
}
