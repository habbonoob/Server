using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>2666 FurnitureAliasesMessageComposer.</summary>
[Message(ServerMessages.FurnitureAliasesMessageComposer)]
public sealed partial record FurnitureAliasesMessageComposer(IReadOnlyList<FurnitureAliasData> Aliases);
