using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>GuideTicketCreationResultMessageComposer — whether a bully report was taken</summary>
[Message(ServerMessages.GuideTicketCreationResultMessageComposer)]
public sealed partial record GuideTicketCreationResultMessageComposer(int Result)
{
    public const int Sent = 0;
    public const int Blocked = 1;

    /// <summary>The reported user said nothing to judge.</summary>
    public const int NoChat = 2;

    public const int AlreadyReported = 3;
}
