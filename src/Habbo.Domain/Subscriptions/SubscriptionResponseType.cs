namespace Habbo.Domain.Subscriptions;

/// <summary>Why the client is being told about a subscription. It branches on the value rather
/// than reading it as a status: only the ones it reacts to are named here.</summary>
public enum SubscriptionResponseType
{
    /// <summary>A plain report of where the membership stands, which is what a login sends.
    /// The client takes the numbers and does nothing else.</summary>
    Info = 0,

    /// <summary>The membership just changed. The client empties and reloads the catalog, so it
    /// is only right to send this after a purchase or a cancellation.</summary>
    Changed = 2,

    /// <summary>The membership is running out. The client flags it as expiring.</summary>
    Expiring = 3,

    /// <summary>The citizenship VIP part of the membership is running out, which the client
    /// tracks apart from the club itself.</summary>
    CitizenshipVipExpiring = 4,
}
