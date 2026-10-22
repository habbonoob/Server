namespace Habbo.Silo.Items.Interactions;

/// <summary>A football goal (fball_goal_*): a ball rolled into it scores for the team its custom params name (1 red,
/// 3 blue), lighting it (state 1) for a moment. Multistate to the client, so without this a double-click would switch
/// it. The room's games run it (<see cref="Rooms.FootballPitch"/>).</summary>
[FurniInteraction(Name)]
public sealed class FootballGoalInteraction : FurniInteraction
{
    public const string Name = "football_goal";
}
