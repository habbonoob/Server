using Habbo.Silo.Abstractions.Hotel;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Tests;

public class RecolorPalettesTests
{
    private sealed class Variables(Dictionary<string, string> values) : IClientVariables
    {
        public Task<string?> GetAsync(string key, CancellationToken ct = default) => Task.FromResult(values.GetValueOrDefault(key));
    }

    private static readonly Variables Hotel = new(new Dictionary<string, string>
    {
        ["recolorable_bc.palette_to_furni"] = "default=bc_block_0_recolorable;wood= bc_plank_recolorable ,bc_log_recolorable",
        ["recolorable_bc.palette.default"] = "FFFFFF,aef0d8,",
        ["recolorable_bc.palette.wood"] = "636568, 7D3822,nothex,ABC",
    });

    [Fact]
    public async Task AFurniTakesThePaletteThatNamesIt()
    {
        Assert.Equal(new HashSet<int> { 0x636568, 0x7D3822 }, await RecolorPalettes.ForAsync(Hotel, "bc_plank_recolorable"));
    }

    [Fact]
    public async Task AFurniNoPaletteNamesTakesTheDefault()
    {
        Assert.Equal(new HashSet<int> { 0xFFFFFF, 0xAEF0D8 }, await RecolorPalettes.ForAsync(Hotel, "bc_cone_recolorable"));
    }

    [Fact]
    public async Task APaletteThatIsNotSetOffersNothing()
    {
        Assert.Empty(await RecolorPalettes.ForAsync(new Variables([]), "bc_cone_recolorable"));
    }
}
