using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Crafting;

/// <summary>CraftSecretEvent</summary>
[Incoming(ClientMessages.CraftSecretEvent)]
public sealed partial record CraftSecretEvent(int AltarId, IReadOnlyList<int> ItemIds);
