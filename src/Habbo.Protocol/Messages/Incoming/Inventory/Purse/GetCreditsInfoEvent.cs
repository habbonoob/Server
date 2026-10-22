using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.Purse;

/// <summary>3719 GetCreditsInfo — the client asks what the purse holds. Answered with
/// CreditBalanceComposer.</summary>
[Incoming(ClientMessages.GetCreditsInfoEvent)]
public sealed partial record GetCreditsInfoEvent;
