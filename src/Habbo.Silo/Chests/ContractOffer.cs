using Habbo.Domain.Furniture.Chests;

namespace Habbo.Silo.Chests;

/// <summary>What a user offers under a contract — credits, and items by kind — measured against its rules. An offer
/// meets a rule when it is that rule exactly, taken some number of times; it may grow towards one while it is no more
/// than the rule taken as often as the contract allows.</summary>
public sealed record ContractOffer(int Credits, IReadOnlyDictionary<ChestKind, int> Kinds)
{
    public bool IsEmpty => Credits == 0 && Kinds.Values.All(count => count == 0);

    /// <summary>How many times the offer meets one of the rules — the most it meets any, 0 when it meets none — and
    /// whether that is what the contract asks for.</summary>
    public (int Times, bool Acceptable) Measure(ContractTerms terms)
    {
        if (terms.IsDonation)
            return IsEmpty ? (0, false) : (1, true);

        var best = (Times: 0, Acceptable: false);
        foreach (var rule in terms.Contract.YouGive ?? [])
        {
            var times = TimesOf(rule);
            var acceptable = times > 0 && terms.RuleType switch
            {
                ContractTerms.Multiplier => times == terms.MaxTimes,
                ContractTerms.AutoMultiplier => times <= terms.MaxTimes,
                _ => times == 1,
            };
            // an acceptable measure beats any that is not; among the rest, the most times met shows the most progress
            if ((acceptable && (!best.Acceptable || times > best.Times)) || (!best.Acceptable && times > best.Times))
                best = (times, acceptable);
        }

        return best;
    }

    /// <summary>Whether the offer can still become one the contract accepts: nothing outside one of its rules, and no
    /// more of anything than that rule taken as often as allowed.</summary>
    public bool Fits(ContractTerms terms) =>
        terms.IsDonation || (terms.Contract.YouGive ?? []).Any(rule => FitsWithin(rule, terms.MaxTimes));

    private int TimesOf(ContractRule rule)
    {
        var (credits, kinds) = Needs(rule);
        if (credits == 0 && kinds.Count == 0)
            return 0;

        var times = credits > 0 ? Credits / credits : Kinds.GetValueOrDefault(kinds.Keys.First()) / kinds.Values.First();
        var exact = times > 0
                    && Credits == credits * times
                    && Kinds.Where(k => k.Value > 0).All(k => kinds.ContainsKey(k.Key))
                    && kinds.All(k => Kinds.GetValueOrDefault(k.Key) == k.Value * times);
        return exact ? times : 0;
    }

    private bool FitsWithin(ContractRule rule, int maxTimes)
    {
        var (credits, kinds) = Needs(rule);
        return Credits <= credits * maxTimes
               && Kinds.Where(k => k.Value > 0).All(k => kinds.TryGetValue(k.Key, out var needed) && k.Value <= needed * maxTimes);
    }

    /// <summary>What a rule asks for: its credits together, and its items by kind.</summary>
    public static (int Credits, Dictionary<ChestKind, int> Kinds) Needs(ContractRule rule)
    {
        var credits = rule.Nodes.Where(n => n.Type == ContractNode.Credits).Sum(n => n.Amount);
        var kinds = rule.Nodes.Where(n => n.Type == ContractNode.Furni)
            .GroupBy(n => new ChestKind(n.IsWallItem, n.TypeId, n.PosterId))
            .ToDictionary(g => g.Key, g => g.Sum(n => n.Amount));
        return (credits, kinds);
    }
}
