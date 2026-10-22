using System.Text.Json;
using Habbo.Infrastructure;

namespace Habbo.Silo.Tests;

/// <summary>The web client reads furnidata as JSON; made from the XML the AIR client reads, it must say the same
/// thing in the hotel's JSON shape.</summary>
public class FurnidataJsonTests
{
    private const string Xml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <furnidata>
          <roomitemtypes>
            <furnitype id="26" classname="chair_silo*4">
              <revision>75469</revision>
              <category>chair</category>
              <xdim>1</xdim><ydim>2</ydim>
              <partcolors><color>#ffffff</color><color>0</color></partcolors>
              <name>Gray &amp; White Chair
              </name>
              <cansiton>1</cansiton>
              <height>1.1125</height>
              <rentofferid>-1</rentofferid>
              <customparams>a,b</customparams>
            </furnitype>
          </roomitemtypes>
          <wallitemtypes>
            <furnitype id="4001" classname="poster">
              <offerid>12</offerid>
              <buyout>0</buyout>
            </furnitype>
          </wallitemtypes>
        </furnidata>
        """;

    private static JsonElement Item(string section, int index) =>
        JsonDocument.Parse(FurnidataJson.FromXml(Xml)).RootElement.GetProperty(section).GetProperty("furnitype")[index];

    [Fact]
    public void ItemsKeepTheirSectionAndOrder()
    {
        var root = JsonDocument.Parse(FurnidataJson.FromXml(Xml)).RootElement;
        Assert.Equal(1, root.GetProperty("roomitemtypes").GetProperty("furnitype").GetArrayLength());
        Assert.Equal(1, root.GetProperty("wallitemtypes").GetProperty("furnitype").GetArrayLength());
    }

    [Fact]
    public void FieldsAreTypedAsTheHotelsJson()
    {
        var chair = Item("roomitemtypes", 0);
        Assert.Equal(26, chair.GetProperty("id").GetInt32());
        Assert.Equal("chair_silo*4", chair.GetProperty("classname").GetString());
        Assert.Equal(75469, chair.GetProperty("revision").GetInt32());
        Assert.Equal(2, chair.GetProperty("ydim").GetInt32());
        Assert.Equal(-1, chair.GetProperty("rentofferid").GetInt32());
        Assert.True(chair.GetProperty("cansiton").GetBoolean());
        Assert.Equal("Gray & White Chair", chair.GetProperty("name").GetString());
        // what is not a count or a flag stays the XML's text
        Assert.Equal("1.1125", chair.GetProperty("height").GetString());
        Assert.Equal("a,b", chair.GetProperty("customparams").GetString());
        Assert.Equal(["#ffffff", "0"], chair.GetProperty("partcolors").GetProperty("color").EnumerateArray().Select(c => c.GetString()!));
    }

    [Fact]
    public void TextIsTrimmedAsTheXmlReaderTrimsIt()
    {
        // the name element ends in a line break and indentation: the client's XML reading drops it
        Assert.Equal("Gray & White Chair", Item("roomitemtypes", 0).GetProperty("name").GetString());
    }

    [Fact]
    public void WallItemsToo()
    {
        var poster = Item("wallitemtypes", 0);
        Assert.Equal(4001, poster.GetProperty("id").GetInt32());
        Assert.Equal(12, poster.GetProperty("offerid").GetInt32());
        Assert.False(poster.GetProperty("buyout").GetBoolean());
    }

    [Fact]
    public void FieldsTheXmlLacksAreLeftOut()
    {
        var poster = Item("wallitemtypes", 0);
        Assert.False(poster.TryGetProperty("tradeable", out _));
        Assert.False(poster.TryGetProperty("partcolors", out _));
    }
}
