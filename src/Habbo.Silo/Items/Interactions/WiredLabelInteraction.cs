namespace Habbo.Silo.Items.Interactions;

/// <summary>A wired label (wf_label): a note on the floor that those who may change the room's wired write, colour and
/// show to everyone or to wired readers only. Its note is kept in furni_labels; the room does the rest.</summary>
[FurniInteraction(Name)]
public sealed class WiredLabelInteraction : FurniInteraction
{
    /// <summary>The interaction name seed/furni_interactions.json gives labels.</summary>
    public const string Name = "wired_label";
}
