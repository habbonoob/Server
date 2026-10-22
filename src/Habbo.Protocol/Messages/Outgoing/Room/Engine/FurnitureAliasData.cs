using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>One entry of FurnitureAliasesMessageComposer.</summary>
[WireStruct]
public sealed partial record FurnitureAliasData(string Name, string Alias);
