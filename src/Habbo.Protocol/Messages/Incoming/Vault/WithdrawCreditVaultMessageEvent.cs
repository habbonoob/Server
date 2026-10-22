using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Vault;

/// <summary>WithdrawCreditVaultMessageEvent — take the credits out of the vault</summary>
[Incoming(ClientMessages.WithdrawCreditVaultMessageEvent)]
public sealed partial record WithdrawCreditVaultMessageEvent;
