namespace Habbo.Silo.Wired.Addons;

/// <summary>Makes the stack's effects run in the order they are stacked, lowest first; without
/// it they run in no particular order.</summary>
[WiredBox("wf_xtra_exec_in_order")]
public sealed class ExecuteInOrderAddon : WiredAddon
{
    public override int Code => AddonCodes.ExecuteInOrder;
}
