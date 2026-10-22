namespace Habbo.Silo.Items.Interactions;

/// <summary>A team gate: walking onto it joins its team (custom params "team,effect"), or leaves it.
/// Its state shows how many are in the team. seed/furni_interactions.json names the gates.</summary>
[FurniInteraction(Name)]
public sealed class GameGateInteraction : FurniInteraction
{
    public const string Name = "game_gate";
}
