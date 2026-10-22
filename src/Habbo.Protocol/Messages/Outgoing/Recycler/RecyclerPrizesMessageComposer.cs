using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Recycler;

/// <summary>RecyclerPrizesMessageComposer</summary>
[Message(ServerMessages.RecyclerPrizesMessageComposer)]
public sealed partial record RecyclerPrizesMessageComposer(IReadOnlyList<RecyclerPrizeLevelData> PrizeLevels);
