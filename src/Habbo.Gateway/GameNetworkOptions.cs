namespace Habbo.Gateway;

public sealed class GameNetworkOptions
{
    public int GamePort { get; set; } = 30000;

    /// <summary>How often each connection is pinged, and how long it may go without answering before it is closed
    /// (longer than the interval). An interval of 0 for no pings.</summary>
    public int PingIntervalSeconds { get; set; }
    public int PongTimeoutSeconds { get; set; }

    /// <summary>How long a connection may stay without logging in; it is closed on the next ping round after (or
    /// within seconds, without pings). 0 for no limit.</summary>
    public int LoginTimeoutSeconds { get; set; } = 60;

    /// <summary>How many game connections one address may hold open at once; 0 for no cap.</summary>
    public int MaxConnectionsPerAddress { get; set; } = 10;

    /// <summary>How many messages a connection may send a second, kept up, and how many at once (the burst a login
    /// or a room entry sends); a connection past both is closed. 0 for no limit.</summary>
    public int MessagesPerSecond { get; set; } = 50;
    public int MessageBurst { get; set; } = 300;
}
