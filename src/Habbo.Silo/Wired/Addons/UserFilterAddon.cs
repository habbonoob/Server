using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Addons;

/// <summary>Cuts the users the stack's selectors chose down to so many (1 to 1000), chosen at random.</summary>
[WiredBox("wf_xtra_filter_users")]
public sealed class UserFilterAddon : WiredAddon
{
    public override int Code => AddonCodes.UserSelectorFilter;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.SelectorFurni];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.SelectorUsers];
    public override IReadOnlyList<int> DefaultIntParams => [10, WiredFilter.FixedAmount, WiredVariableTargets.User];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;
}
