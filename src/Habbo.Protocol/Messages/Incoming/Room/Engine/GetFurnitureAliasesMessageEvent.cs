using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>1952 GetFurnitureAliases.</summary>
[Incoming(ClientMessages.GetFurnitureAliasesMessageEvent)]
public sealed partial record GetFurnitureAliasesMessageEvent;
