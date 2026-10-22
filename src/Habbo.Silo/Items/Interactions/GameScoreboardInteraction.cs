namespace Habbo.Silo.Items.Interactions;

/// <summary>A team scoreboard (the client's furniture_score): it shows the score of the team its
/// custom params name. The room's games set it; nobody clicks it.</summary>
[FurniInteraction(Name)]
public sealed class GameScoreboardInteraction : FurniInteraction
{
    public const string Name = "furniture_score";
}
