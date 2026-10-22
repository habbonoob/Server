namespace Habbo.Silo.Users;

/// <summary>Notifications kept for players while they are away (config section "DeferredNotifications").</summary>
public sealed class DeferredNotificationOptions
{
    /// <summary>How many a player is shown when they come back; older ones are dropped.</summary>
    public int DeliveredOnLogin { get; set; }
}
