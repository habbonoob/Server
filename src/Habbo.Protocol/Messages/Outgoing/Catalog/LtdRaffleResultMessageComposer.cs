using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>1593 LtdRaffleResultMessageComposer — how the raffle for a limited edition went for the player.</summary>
[Message(ServerMessages.LtdRaffleResultMessageComposer)]
public sealed partial record LtdRaffleResultMessageComposer(string ClassName, byte ResultCode)
{
    public const byte Won = 0;
    public const byte Lost = 1;
}
