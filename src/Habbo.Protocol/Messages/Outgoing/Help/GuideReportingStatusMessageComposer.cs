using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>GuideReportingStatusMessageComposer — whether the player may ask for help or report now</summary>
[Message(ServerMessages.GuideReportingStatusMessageComposer)]
public sealed partial record GuideReportingStatusMessageComposer(int StatusCode, [property: WireWhen("HasPendingTicket")] PendingGuideTicketData? PendingTicket)
{
    /// <summary>Free to ask.</summary>
    public const int Ok = 0;

    /// <summary>A request of theirs is still open.</summary>
    public const int Pending = 1;

    /// <summary>Barred from reporting.</summary>
    public const int Blocked = 2;

    /// <summary>Reported too recently.</summary>
    public const int TooQuick = 3;

    public bool HasPendingTicket => StatusCode == Pending;
}
