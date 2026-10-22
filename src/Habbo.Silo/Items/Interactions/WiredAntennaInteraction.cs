namespace Habbo.Silo.Items.Interactions;

/// <summary>A wired antenna: what "send signal" sends through and "receive signal" listens on. It
/// flashes when a signal passes; nobody switches it by hand.</summary>
[FurniInteraction(Name)]
public sealed class WiredAntennaInteraction : FurniInteraction
{
    /// <summary>The interaction name seed/furni_interactions.json gives antennas.</summary>
    public const string Name = "wired_antenna";
}
