using System.Globalization;
using Habbo.Silo.Abstractions.Hotel;

namespace Habbo.Silo.Chests;

/// <summary>Reads the chest limits from the client's variables, once.</summary>
public sealed class ChestRules(IClientVariables variables)
{
    private ChestLimits? _limits;

    public async Task<ChestLimits> GetAsync()
    {
        if (_limits is { } known)
            return known;

        async Task<int> Number(string key) =>
            int.TryParse(await variables.GetAsync(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;

        return _limits = new ChestLimits(
            await Number("wired.furni_chest.initial_capacity"), await Number("wired.furni_chest.upgrade_capacity"),
            await Number("wired.furni_chest.max_upgrades"), await Number("wired.furni_chest.starter_capacity"),
            await Number("wired.coins_chest.initial_capacity"), await Number("wired.coins_chest.upgrade_capacity"),
            await Number("wired.coins_chest.max_upgrades"), await Number("wired.coins_chest.starter_capacity"),
            await Number("wired.chests.upgrade_cost_credits"), await Number("wired.chests.upgrade_cost_diamonds"),
            await variables.GetAsync("wired.chests_starter_infix") ?? string.Empty, await Number("wired.chests_max_logs"));
    }
}
