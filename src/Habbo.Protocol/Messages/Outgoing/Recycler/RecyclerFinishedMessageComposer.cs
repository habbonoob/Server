using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Recycler;

/// <summary>RecyclerFinishedMessageComposer — 1 done, 2 failed</summary>
[Message(ServerMessages.RecyclerFinishedMessageComposer)]
public sealed partial record RecyclerFinishedMessageComposer(int RecyclerFinishedStatus, int PrizeId)
{
    public const int Done = 1;
    public const int Failed = 2;
}
