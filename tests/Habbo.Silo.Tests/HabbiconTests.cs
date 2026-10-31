using System.Buffers;
using System.Security.Cryptography;
using Habbo.Infrastructure;
using Habbo.Protocol.Composers;
using Habbo.Protocol.IO;
using Habbo.Protocol.Messages.Outgoing.Habbicons;
using Habbo.Silo.Habbicons;
using Microsoft.Extensions.Configuration;

namespace Habbo.Silo.Tests;

public class HabbiconTests
{
    /// <summary>The account habbo.com sent its Habbicons to on 25 Oct 2026: 23 owned, none starred.</summary>
    private static readonly int[] CapturedOwned =
        [28, 29, 30, 31, 32, 33, 35, 34, 36, 37, 41, 40, 39, 42, 44, 43, 50, 51, 52, 53, 54, 55, 56];

    private static readonly int[] CapturedRecent = [43, 42, 41, 55, 56, 53, 35, 36, 33, 34];

    private static Dictionary<int, bool> Owned(params int[] ids) => ids.ToDictionary(id => id, _ => false);

    private static Task<IReadOnlyList<HabbiconCollection>> SeedAsync() =>
        new HabbiconCatalog(new SeedFiles(new ConfigurationBuilder().Build())).AllAsync();

    private static byte[] Payload(IComposer composer)
    {
        var buffer = new ArrayBufferWriter<byte>();
        composer.Compose(new MessageWriter(buffer));
        return buffer.WrittenSpan.ToArray();
    }

    [Fact]
    public async Task TheSeedIsHabbosFiveSetsOfFiftyFiveHabbicons()
    {
        var collections = await SeedAsync();

        Assert.Equal(["duck", "duck2", "frank", "toast", "zombie"], collections.Select(c => c.Name));
        Assert.Equal(55, collections.Sum(c => c.Habbicons.Count + (c.Reward is null ? 0 : 1)));
        var toast = collections.Single(c => c.Name == "toast");
        Assert.Equal((50, 50, 5), (toast.PriceCredits, toast.PriceActivityPoints, toast.ActivityPointType));
        Assert.All(toast.Habbicons, h => Assert.Equal((5, 5, 5), (h.PriceCredits, h.PriceActivityPoints, h.ActivityPointType)));
        Assert.All(collections.Where(c => c != toast).SelectMany(c => c.Habbicons), h => Assert.False(h.IsSold));
    }

    [Fact]
    public async Task TheCapturedAccountGetsWhatHabboSentIt()
    {
        var collections = await SeedAsync();
        var owned = Owned(CapturedOwned);

        var shop = Payload(new HabbiconShopDataComposer(collections.Select(c => HabbiconRules.DataOf(c, owned)).ToList()));
        Assert.Equal(2041, shop.Length);
        Assert.Equal("d89e2cc1cf09dec51bd67d1b04d8782764c136446e20c77441f80defe00699f2", Convert.ToHexStringLower(SHA256.HashData(shop)));

        var mine = Payload(new UserHabbiconsComposer(HabbiconRules.OwnedData(collections, owned), CapturedRecent));
        Assert.Equal("f53d91fc5181a50ec0232b95bf4c68eb7979aa851927e622e4d31061f7b84f9c", Convert.ToHexStringLower(SHA256.HashData(mine)));
    }

    [Fact]
    public async Task ASetsRewardIsLockedUntilTheSetIsCompleteThenClaimableOnce()
    {
        var duck = (await SeedAsync()).Single(c => c.Name == "duck");
        var all = duck.Habbicons.Select(h => h.Id).ToArray();

        Assert.Equal(HabbiconState.Locked, HabbiconRules.RewardStateOf(duck, Owned(all[..^1])));
        Assert.False(HabbiconRules.CanClaim(duck, duck.Reward!.Id, Owned(all[..^1])));

        Assert.Equal(HabbiconState.Claimable, HabbiconRules.RewardStateOf(duck, Owned(all)));
        Assert.True(HabbiconRules.CanClaim(duck, duck.Reward.Id, Owned(all)));
        Assert.False(HabbiconRules.CanClaim(duck, all[0], Owned(all)));

        var claimed = Owned([.. all, duck.Reward.Id]);
        Assert.Equal(HabbiconState.Owned, HabbiconRules.RewardStateOf(duck, claimed));
        Assert.False(HabbiconRules.CanClaim(duck, duck.Reward.Id, claimed));
    }

    [Fact]
    public async Task OnlyAHabbiconForSaleAndNotOwnedCanBeBought()
    {
        var collections = await SeedAsync();
        var toast = collections.Single(c => c.Name == "toast");
        var duck = collections.Single(c => c.Name == "duck");

        Assert.True(HabbiconRules.CanBuy(toast, toast.Habbicons[0], Owned()));
        Assert.False(HabbiconRules.CanBuy(toast, toast.Habbicons[0], Owned(toast.Habbicons[0].Id)));
        // given, never sold: a reward track's prize, and a set's reward
        Assert.Equal(HabbiconState.Reward, HabbiconRules.StateOf(duck.Habbicons[0], Owned()));
        Assert.False(HabbiconRules.CanBuy(duck, duck.Habbicons[0], Owned()));
        Assert.False(HabbiconRules.CanBuy(toast, toast.Reward!, Owned()));
    }

    [Fact]
    public async Task BuyingASetHandsOverWhatIsMissingUntilItIsComplete()
    {
        var collections = await SeedAsync();
        var toast = collections.Single(c => c.Name == "toast");
        var first = toast.Habbicons[0].Id;

        Assert.Equal(toast.Habbicons.Skip(1).Select(h => h.Id), HabbiconRules.SetPurchase(toast, Owned(first)).Select(h => h.Id));
        Assert.Empty(HabbiconRules.SetPurchase(toast, Owned(toast.Habbicons.Select(h => h.Id).ToArray())));
        Assert.Empty(HabbiconRules.SetPurchase(collections.Single(c => c.Name == "duck"), Owned()));
    }

    [Fact]
    public async Task AStarredHabbiconIsAFavourite()
    {
        var collections = await SeedAsync();
        var duck = collections[0];
        var owned = new Dictionary<int, bool> { [duck.Habbicons[0].Id] = true, [duck.Habbicons[1].Id] = false };

        Assert.Equal(HabbiconState.Favourite, HabbiconRules.StateOf(duck.Habbicons[0], owned));
        Assert.Equal(
            [new UserHabbiconData(duck.Habbicons[0].Id, HabbiconState.Favourite), new UserHabbiconData(duck.Habbicons[1].Id, HabbiconState.Owned)],
            HabbiconRules.OwnedData(collections, owned));
    }
}
