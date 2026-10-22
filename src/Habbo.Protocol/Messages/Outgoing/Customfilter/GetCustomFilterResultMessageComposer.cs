using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Customfilter;

/// <summary>GetCustomFilterResultMessageComposer — the player's own chat filter</summary>
[Message(ServerMessages.GetCustomFilterResultMessageComposer)]
public sealed partial record GetCustomFilterResultMessageComposer(IReadOnlyList<string> Words);
