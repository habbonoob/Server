namespace Habbo.Silo.Items.Interactions;

/// <summary>A Battle Banzai tile (bb_patch*): a game colours it as its team's players step on it. Its state is
/// 0 off, 1 lit for a game, then 3 × team + level, level 2 locked. The room's games run it
/// (<see cref="Rooms.BanzaiArena"/>); seed/furni_interactions.json names the tiles.</summary>
[FurniInteraction(Name)]
public sealed class BanzaiTileInteraction : FurniInteraction
{
    public const string Name = "banzai_tile";
}
