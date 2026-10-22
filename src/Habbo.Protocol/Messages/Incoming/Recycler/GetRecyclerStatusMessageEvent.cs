using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Recycler;

/// <summary>GetRecyclerStatusMessageEvent</summary>
[Incoming(ClientMessages.GetRecyclerStatusMessageEvent)]
public sealed partial record GetRecyclerStatusMessageEvent;
