namespace Habbo.Silo.Wired.Addons;

/// <summary>Gives the stack's texts a placeholder, $(name), for the names of its furni: the first,
/// or all of them with the delimiter between.</summary>
[WiredBox("wf_xtra_text_output_furni_name")]
public sealed class FurniNamePlaceholderAddon : WiredPlaceholderAddon
{
    public override int Code => AddonCodes.FurniNamePlaceholder;
    public override bool AllowsWallFurni => true;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.Furni.DefaultingTo(WiredSources.TriggerFurni)];

    protected override IEnumerable<string> Values(WiredBox box, WiredRun run) =>
        run.Furni(box).Select(item => item.Item.Definition!.Name);
}
