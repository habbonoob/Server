namespace Habbo.Silo.Wired.Addons;

/// <summary>Makes the stack's moves happen at once, without animating.</summary>
[WiredBox("wf_xtra_mov_no_animation")]
public sealed class NoMoveAnimationAddon : WiredAddon
{
    public override int Code => AddonCodes.NoMoveAnimation;
}
