using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Perk;

/// <summary>One entry of PerkAllowancesMessageComposer.</summary>
[WireStruct]
public sealed partial record PerkAllowanceData(string Code, string ErrorMessage, bool IsAllowed);
