using System.Text.Json;
using Habbo.Silo.Marketplace;
using Habbo.Silo.Navigator;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Tests;

/// <summary>The settings the hotel ships with that Habbo's own values decide: the option's default and
/// src/Habbo.Host/appsettings.json both give Habbo's.</summary>
public class HotelDefaultsTests
{
    private static readonly Lazy<JsonElement> AppSettings = new(() =>
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Habbo.slnx")))
            directory = directory.Parent;

        var path = Path.Combine(directory?.FullName ?? throw new InvalidOperationException("No Habbo.slnx above the tests."),
            "src", "Habbo.Host", "appsettings.json");
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        return json.RootElement.Clone();
    });

    public static TheoryData<string, string, int, int> Defaults => new()
    {
        { "Navigator", nameof(NavigatorOptions.ShowMoreResultLimit), new NavigatorOptions().ShowMoreResultLimit, 75 },
        { "Rooms", nameof(RoomOptions.IdleAutokickSeconds), new RoomOptions().IdleAutokickSeconds, 900 },
        { "Marketplace", nameof(MarketplaceOptions.ListLimit), new MarketplaceOptions().ListLimit, 250 },
    };

    [Theory]
    [MemberData(nameof(Defaults))]
    public void ShipsWithHabbosValue(string section, string key, int optionDefault, int habbo)
    {
        Assert.Equal(habbo, optionDefault);
        Assert.Equal(habbo, AppSettings.Value.GetProperty(section).GetProperty(key).GetInt32());
    }
}
