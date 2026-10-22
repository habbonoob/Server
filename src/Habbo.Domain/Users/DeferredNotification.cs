namespace Habbo.Domain.Users;

/// <summary>A notification kept for a player who was offline when it happened, shown when they next log in.</summary>
public sealed class DeferredNotification
{
    public long Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>The notification's type, the key of its "notification.&lt;type&gt;" setting.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Its parameters, keys and values in the same order.</summary>
    public List<string> ParameterKeys { get; set; } = [];

    public List<string> ParameterValues { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
