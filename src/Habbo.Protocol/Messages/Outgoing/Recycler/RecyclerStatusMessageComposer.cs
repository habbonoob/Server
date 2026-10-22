using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Recycler;

/// <summary>RecyclerStatusMessageComposer — 1 ready, 2 closed, 3 cooling down for the seconds given</summary>
[Message(ServerMessages.RecyclerStatusMessageComposer)]
public sealed partial record RecyclerStatusMessageComposer(int RecyclerStatus, int RecyclerTimeoutSeconds)
{
    public const int Ready = 1;
    public const int Closed = 2;
    public const int CoolingDown = 3;
}
