using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Achievements;

namespace Habbo.Protocol.Messages.Outgoing.Perk;

/// <summary>3291 PerkAllowancesMessageComposer.</summary>
[Message(ServerMessages.PerkAllowancesMessageComposer)]
public sealed partial record PerkAllowancesMessageComposer(IReadOnlyList<PerkAllowanceData> Allowances)
{
    public static PerkAllowancesMessageComposer From(IReadOnlyList<PerkAllowance> allowances) =>
        new(allowances
            .Select(allowance => new PerkAllowanceData(allowance.Code, allowance.ErrorMessage, allowance.IsAllowed))
            .ToList());
}
