namespace Habbo.Silo.Wired.Addons;

/// <summary>Makes users ride along with the furni the stack moves: those standing on it, or
/// anyone on its tiles, as the dialog chooses.</summary>
[WiredBox("wf_xtra_mov_carry_users")]
public sealed class CarryUsersAddon : WiredAddon
{
    // carry_mode.0 and .1
    public const int StandingOnIt = 0;
    public const int OnItsTiles = 1;

    public override int Code => AddonCodes.CarryUsers;
    public override IReadOnlyList<int> DefaultIntParams => [StandingOnIt];
}
