namespace Habbo.Silo.Wired.Addons;

/// <summary>Gives the stack's texts a placeholder, $(name), for the names of its users: the first,
/// or all of them with the delimiter between.</summary>
[WiredBox("wf_xtra_text_output_username")]
public sealed class UsernamePlaceholderAddon : WiredPlaceholderAddon
{
    public override int Code => AddonCodes.UsernamePlaceholder;
    public override bool TargetsUsers => true;

    protected override IEnumerable<string> Values(WiredBox box, WiredRun run) =>
        run.Users(box).Select(user => user.Player.Username);
}
