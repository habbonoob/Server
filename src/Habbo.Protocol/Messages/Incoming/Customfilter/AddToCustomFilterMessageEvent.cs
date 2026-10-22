using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Customfilter;

/// <summary>AddToCustomFilterMessageEvent — filter a word out of chat</summary>
[Incoming(ClientMessages.AddToCustomFilterMessageEvent)]
public sealed partial record AddToCustomFilterMessageEvent(string Word);
