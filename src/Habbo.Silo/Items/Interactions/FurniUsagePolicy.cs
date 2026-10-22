namespace Habbo.Silo.Items.Interactions;

/// <summary>Who may use an item. Sent with every item: the client shows the infostand's Use
/// button by it, and in wired play-test mode refuses to send a use below Everyone.</summary>
public enum FurniUsagePolicy
{
    /// <summary>Only wired may change it.</summary>
    Nobody = 0,

    /// <summary>The room's owner and anyone with rights.</summary>
    Controllers = 1,

    Everyone = 2,
}
