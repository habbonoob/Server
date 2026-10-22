using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>GuideTicketResolutionMessageComposer — what the guardians made of a bully report</summary>
[Message(ServerMessages.GuideTicketResolutionMessageComposer)]
public sealed partial record GuideTicketResolutionMessageComposer(int Resolution)
{
    /// <summary>The suspect behaved badly, or awfully: the report stood.</summary>
    public const int Bullying = 0;
    public const int SevereBullying = 1;

    /// <summary>The suspect behaved acceptably: the report did not stand.</summary>
    public const int Acceptable = 2;
}
