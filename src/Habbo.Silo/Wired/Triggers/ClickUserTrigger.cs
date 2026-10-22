namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires when a user clicks another; the clicker is the triggering user, and the one
/// clicked is the clicked user. The box may keep the avatar menu from opening, and the clicker
/// from turning to face who they clicked.</summary>
[WiredBox("wf_trg_click_user")]
public sealed class ClickUserTrigger : WiredTrigger
{
    private const int BlockMenu = 0;
    private const int DoNotRotate = 1;

    public override int Code => TriggerCodes.UserClicksUser;
    public override IReadOnlyList<int> DefaultIntParams => [1, 0];

    public static bool BlocksMenu(WiredBox box) => box.Flag(BlockMenu);
    public static bool KeepsDirection(WiredBox box) => box.Flag(DoNotRotate);

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room) =>
        e is { Type: WiredEventType.UserClicked, ClickedUser: not null };
}
