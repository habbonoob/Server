namespace Habbo.Silo.Bots;

/// <summary>Bots (config section "Bots").</summary>
public sealed class BotOptions
{
    /// <summary>Whether the hotel lets bots into rooms at all.</summary>
    public bool Enabled { get; set; }

    /// <summary>How many bots one room may hold.</summary>
    public int MaxPerRoom { get; set; }

    /// <summary>The bounds a bot's chat delay is kept within, in seconds.</summary>
    public int MinChatDelaySeconds { get; set; }
    public int MaxChatDelaySeconds { get; set; }

    /// <summary>How many sentences a bot keeps, and how long each may be.</summary>
    public int MaxChatLines { get; set; }
    public int MaxChatLineLength { get; set; }

    /// <summary>How long a bot that walks about stands still between walks, in seconds.</summary>
    public int RandomWalkPauseSeconds { get; set; }

    /// <summary>How far away, in tiles, a bot that serves hears an order.</summary>
    public int ServeReach { get; set; }

    /// <summary>The chat bubble bots talk in, by its asset name.</summary>
    public string ChatStyleAsset { get; set; } = string.Empty;

    /// <summary>What a bot that serves says handing over a drink: %username% and %item% are filled in.</summary>
    public string ServedText { get; set; } = string.Empty;

    /// <summary>What a visitor logger tells a visitor: %username% and %owner% are filled in.</summary>
    public string VisitorGreetingText { get; set; } = string.Empty;

    /// <summary>What a visitor logger tells the owner coming back: %visitors% is the list of names.</summary>
    public string VisitorsReportText { get; set; } = string.Empty;

    /// <summary>What a visitor logger tells the owner when nobody came.</summary>
    public string NoVisitorsText { get; set; } = string.Empty;
}
