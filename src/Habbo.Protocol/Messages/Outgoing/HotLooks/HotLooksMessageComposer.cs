using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.HotLooks;

/// <summary>3755 HotLooksMessageComposer.</summary>
[Message(ServerMessages.HotLooksMessageComposer)]
public sealed partial record HotLooksMessageComposer(IReadOnlyList<HotLookData> Looks);
