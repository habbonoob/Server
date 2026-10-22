using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Customfilter;

/// <summary>GetCustomFilterMessageEvent — the player's own chat filter</summary>
[Incoming(ClientMessages.GetCustomFilterMessageEvent)]
public sealed partial record GetCustomFilterMessageEvent;
