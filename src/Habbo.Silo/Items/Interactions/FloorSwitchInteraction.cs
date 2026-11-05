namespace Habbo.Silo.Items.Interactions;

/// <summary>A wired floor switch (wf_floor_switch1, wf_floor_switch2): multistate to the client, stepped through
/// on use like a lamp, but furnidata says it "can only be operated from an adjacent tile", and Toggle Furni State,
/// which switches a furni as a double click would, leaves it alone.</summary>
[FurniInteraction(Name)]
public sealed class FloorSwitchInteraction : FurniInteraction
{
    /// <summary>The interaction name seed/furni_interactions.json gives floor switches.</summary>
    public const string Name = "wired_floor_switch";

    private readonly MultiStateInteraction _states = new();

    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Controllers;

    public override Task UseAsync(FurniUse use) => _states.UseAsync(use);
}
