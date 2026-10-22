using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Recycler;

/// <summary>GetRecyclerPrizesMessageEvent</summary>
[Incoming(ClientMessages.GetRecyclerPrizesMessageEvent)]
public sealed partial record GetRecyclerPrizesMessageEvent;
