using System.Text.Json;
using Habbo.Domain.Users;
using Habbo.Silo.Perks;

namespace Habbo.Silo.Tests;

/// <summary>Perks as habbo.com sends them (PerkAllowances, 4 November 2026, an ordinary account): thirteen codes, an
/// allowed perk with an empty message and a denied one with the requirement it lacks.</summary>
public class PerkAllowanceTests
{
    [Fact]
    public void ADeniedPerkCarriesItsRequirementAndAnAllowedOneNothing()
    {
        var perk = new Perk { Code = "USE_GUIDE_TOOL", ErrorMessage = "requirement.unfulfilled.helper_level_4", MinimumRank = SecurityLevel.Employee };

        var denied = PerkService.Allowance(perk, SecurityLevel.None);
        Assert.False(denied.IsAllowed);
        Assert.Equal("requirement.unfulfilled.helper_level_4", denied.ErrorMessage);

        var allowed = PerkService.Allowance(perk, SecurityLevel.Employee);
        Assert.True(allowed.IsAllowed);
        Assert.Equal(string.Empty, allowed.ErrorMessage);
    }

    [Fact]
    public void TheSeedHasHabbosPerks()
    {
        var file = Path.Combine(RepoRoot(), "seed", "perks.json");
        using var json = JsonDocument.Parse(File.ReadAllText(file));
        var codes = json.RootElement.EnumerateArray().Select(p => p.GetProperty("code").GetString()).ToHashSet();

        string[] habbo =
        [
            "NAVIGATOR_PHASE_TWO_2014", "CITIZEN", "HABBO_CLUB_OFFER_BETA", "USE_GUIDE_TOOL", "MOUSE_ZOOM",
            "NAVIGATOR_ROOM_THUMBNAIL_CAMERA", "JUDGE_CHAT_REVIEWS", "TRADE", "VOTE_IN_COMPETITIONS", "UNITY_TRADE",
            "BUILDER_AT_WORK", "CAMERA", "CALL_ON_HELPERS",
        ];
        Assert.Equal(habbo.ToHashSet(), codes);
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Habbo.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("no Habbo.slnx above the test output");
    }
}
