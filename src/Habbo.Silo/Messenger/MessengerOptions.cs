namespace Habbo.Silo.Messenger;

/// <summary>The console's rules (config section "Messenger").</summary>
public sealed class MessengerOptions
{
    /// <summary>Messages per page when scrolling back through a conversation.</summary>
    public int HistoryPageSize { get; set; } = 20;

    /// <summary>The most users one search returns.</summary>
    public int SearchResultLimit { get; set; } = 50;

    /// <summary>How long a player waits between one console message and the next; one sent sooner is dropped.</summary>
    public int MessageIntervalMilliseconds { get; set; } = 500;
}
