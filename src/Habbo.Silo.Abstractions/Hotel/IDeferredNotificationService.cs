namespace Habbo.Silo.Abstractions.Hotel;

/// <summary>Notifications that reach a player now if they are online, or when they next log in.</summary>
public interface IDeferredNotificationService
{
    /// <summary>A notification of a type the client's "notification.&lt;type&gt;" setting describes, with its parameters.</summary>
    Task NotifyAsync(Guid playerId, string type, IReadOnlyDictionary<string, string> parameters);

    /// <summary>What was kept for the player while they were away, the newest up to the hotel's limit.</summary>
    Task DeliverAsync(Guid playerId);
}
