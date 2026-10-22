using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Handshake;

/// <summary>1019 NoobnessLevelMessageComposer — how new the client should treat the player as.
/// It decides whether the new-user help is shown.</summary>
[Message(ServerMessages.NoobnessLevelMessageComposer)]
public sealed partial record NoobnessLevelMessageComposer(int Level)
{
    /// <summary>The client's NoobnessLevelEnum. A player who has been around is neither a new
    /// identity nor a real noob, and the client shows them nothing extra.</summary>
    public const int Established = 0;

    public const int NewIdentity = 1;

    public const int RealNoob = 2;
}
