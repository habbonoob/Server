using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Vault;

/// <summary>CreditVaultStatusMessageEvent — the player's credit vault</summary>
[Incoming(ClientMessages.CreditVaultStatusMessageEvent)]
public sealed partial record CreditVaultStatusMessageEvent;
