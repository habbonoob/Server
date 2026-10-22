namespace Habbo.Silo.Games.SnowWar;

/// <summary>How the hotel runs SnowStorm: how many play, how long each part lasts, how often machines refill and
/// players take snowballs from them, and how many games a day are free.</summary>
public sealed class SnowWarOptions
{
    /// <summary>Whether the game center lets anyone play.</summary>
    public bool Enabled { get; set; } = true;

    public int Teams { get; set; } = 2;

    /// <summary>How many must be waiting before the countdown starts, and how many a game takes.</summary>
    public int MinPlayers { get; set; } = 2;
    public int MaxPlayers { get; set; } = 10;

    public int LobbyCountdownSeconds { get; set; } = 10;

    /// <summary>How long everyone has to load the arena; whoever has not by then is left behind.</summary>
    public int StageLoadTimeoutSeconds { get; set; } = 30;

    /// <summary>The countdown before a round, and the round itself.</summary>
    public int StageCountdownSeconds { get; set; } = 5;
    public int GameSeconds { get; set; } = 120;

    /// <summary>How long the results stay up, and those who want to play again have to say so.</summary>
    public int ResultsSeconds { get; set; } = 20;

    /// <summary>Every so many turns (of 150 milliseconds) a machine makes a snowball.</summary>
    public int MachineSnowballIntervalTurns { get; set; } = 20;

    /// <summary>Every so many turns a player standing next to a machine or a pile takes a snowball from it.</summary>
    public int PickupIntervalTurns { get; set; } = 3;

    /// <summary>Free games a day; 0 for as many as one likes.</summary>
    public int FreeGamesPerDay { get; set; }

    /// <summary>Whether club members play as often as they like.</summary>
    public bool ClubPlaysUnlimited { get; set; } = true;

    /// <summary>How many games may run at once; lobbies whose countdown ends beyond that wait in a queue for an arena.
    /// 0 for no limit.</summary>
    public int MaxRunningGames { get; set; }

    /// <summary>How long one who walks out of a game before it ends must wait to play again.</summary>
    public int QuitBlockSeconds { get; set; } = 60;

    /// <summary>The token offers the catalog sells for games beyond the free ones; none by default.</summary>
    public List<SnowWarTokenOffer> TokenOffers { get; set; } = [];
}
