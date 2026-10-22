using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Addons;

/// <summary>Makes every move of its stack arc, at a strength (-1000 to 1000) from the dialog or from a variable: the
/// furni curve and the users moved along jump. It goes over a projectile's own curve.</summary>
[WiredBox("wf_xtra_mov_curve")]
public sealed class MovementCurveAddon : WiredAddon
{
    private const int Operand = 0;
    private const int Strength = 1;
    private const int VariableTarget = 2;
    private const int FromVariable = 1;
    private const int MaxStrength = 1000;

    public override int Code => AddonCodes.JumpStrength;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.Furni];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Users];
    public override IReadOnlyList<int> DefaultIntParams => [0, 160, WiredVariableTargets.Context];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    /// <summary>The strength its stack's moves arc with; null when its variable has no value to give.</summary>
    public static int? CurveStrength(WiredBox box, WiredRun run) =>
        (box.IntParam(Operand) == FromVariable
            ? WiredVariableUse.Reference(box, run, fromVariable: true, number: 0, box.IntParam(VariableTarget), variableIndex: 0, slot: 0)
            : box.IntParam(Strength)) is { } strength
            ? Math.Clamp(strength, -MaxStrength, MaxStrength)
            : null;
}
