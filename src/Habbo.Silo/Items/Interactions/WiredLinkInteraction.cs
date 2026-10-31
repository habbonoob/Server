namespace Habbo.Silo.Items.Interactions;

/// <summary>A WIRED Link (wf_link): what the send-link effects send through and the receive-link triggers listen on,
/// connected to its owner's network in its dialog. Using it opens that dialog for its owner, which the room does
/// itself; it flashes as messages pass, and nobody switches it by hand.</summary>
[FurniInteraction(Name)]
public sealed class WiredLinkInteraction : FurniInteraction
{
    /// <summary>The interaction name seed/furni_interactions.json gives Links.</summary>
    public const string Name = "wired_link";
}
