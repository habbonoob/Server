using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Customfilter;

/// <summary>RemoveFromCustomFilterMessageEvent — stop filtering a word</summary>
[Incoming(ClientMessages.RemoveFromCustomFilterMessageEvent)]
public sealed partial record RemoveFromCustomFilterMessageEvent(string Word);
