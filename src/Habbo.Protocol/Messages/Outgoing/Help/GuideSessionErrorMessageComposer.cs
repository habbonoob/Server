using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>GuideSessionErrorMessageComposer — the request could not be handled</summary>
[Message(ServerMessages.GuideSessionErrorMessageComposer)]
public sealed partial record GuideSessionErrorMessageComposer(int ErrorCode)
{
    public const int Generic = 0;

    /// <summary>No guide took it.</summary>
    public const int Rejected = 1;

    /// <summary>Not enough guardians for a bully report.</summary>
    public const int NotEnoughGuardians = 2;
}
