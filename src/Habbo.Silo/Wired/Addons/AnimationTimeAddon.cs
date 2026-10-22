namespace Habbo.Silo.Wired.Addons;

/// <summary>Sets how long the stack's moves take to animate, in milliseconds (50 to 2000).</summary>
[WiredBox("wf_xtra_anim_time")]
public sealed class AnimationTimeAddon : WiredAddon
{
    public const int MinMilliseconds = 50;
    public const int MaxMilliseconds = 2000;

    public override int Code => AddonCodes.AnimationTime;
    public override IReadOnlyList<int> DefaultIntParams => [WiredMoveOptions.DefaultAnimationMilliseconds];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public static int Milliseconds(WiredBox box) =>
        Math.Clamp(box.IntParam(0, WiredMoveOptions.DefaultAnimationMilliseconds), MinMilliseconds, MaxMilliseconds);
}
