using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Addons;

/// <summary>Cuts the furni the stack's selectors chose down to so many (1 to 1000), chosen at random.</summary>
[WiredBox("wf_xtra_filter_furni")]
public sealed class FurniFilterAddon : WiredAddon
{
    public override int Code => AddonCodes.FurniSelectorFilter;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.SelectorFurni];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.SelectorUsers];
    public override IReadOnlyList<int> DefaultIntParams => [10, WiredFilter.FixedAmount, WiredVariableTargets.User];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;
}
