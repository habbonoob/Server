using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Misc;

/// <summary>SelfDonationResultMessageComposer — how giving oneself items went</summary>
[Message(ServerMessages.SelfDonationResultMessageComposer)]
public sealed partial record SelfDonationResultMessageComposer(int ResultCode)
{
    /// <summary>The results the client has texts for ("selfdonation.result.*").</summary>
    public const int Success = 0;
    public const int NotAllowed = 1;
    public const int Failed = 2;
}
