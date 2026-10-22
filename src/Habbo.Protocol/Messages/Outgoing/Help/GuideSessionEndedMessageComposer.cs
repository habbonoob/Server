using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>GuideSessionEndedMessageComposer — the session is over, and why</summary>
[Message(ServerMessages.GuideSessionEndedMessageComposer)]
public sealed partial record GuideSessionEndedMessageComposer(int EndReason)
{
    /// <summary>The other side went away.</summary>
    public const int PartnerLeft = 0;

    /// <summary>The other side ended it.</summary>
    public const int EndedByPartner = 1;

    /// <summary>The receiver ended it.</summary>
    public const int EndedByYou = 2;
}
