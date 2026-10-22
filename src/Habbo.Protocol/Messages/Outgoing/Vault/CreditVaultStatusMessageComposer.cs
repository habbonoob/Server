using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Vault;

/// <summary>CreditVaultStatusMessageComposer — the player's credit vault</summary>
[Message(ServerMessages.CreditVaultStatusMessageComposer)]
public sealed partial record CreditVaultStatusMessageComposer(bool IsUnlocked, int TotalBalance, int WithdrawBalance);
