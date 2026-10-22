using System.Text;
using Habbo.Domain.Furniture;
using Habbo.Infrastructure;

namespace Habbo.Silo.Tests;

/// <summary>furnidata is the client's own furniture list, so a field we read wrongly produces
/// furniture that renders one way and behaves another.</summary>
public class FurnidataParserTests
{
    private static List<ItemDefinition> Parse(string xml) =>
        FurnidataParser.Parse(new MemoryStream(Encoding.UTF8.GetBytes(xml))).ToList();

    private const string TwoItems = """
        <furnidata>
          <roomitemtypes>
            <furnitype id="26" classname="chair_silo">
              <revision>75469</revision>
              <category>chair</category>
              <defaultdir>2</defaultdir>
              <xdim>1</xdim><ydim>2</ydim>
              <name>Gray Dining Chair</name>
              <description>Keep it simple.</description>
              <specialtype>1</specialtype>
              <canstandon>0</canstandon>
              <cansiton>1</cansiton>
              <canlayon>0</canlayon>
              <canputstuffon>1</canputstuffon>
              <height>1.1125</height>
              <furniline>area</furniline>
              <rare>0</rare>
              <tradeable>1</tradeable>
              <recyclable>0</recyclable>
            </furnitype>
          </roomitemtypes>
          <wallitemtypes>
            <furnitype id="4001" classname="poster">
              <category>wall_decoration</category>
              <specialtype>6</specialtype>
              <rare>1</rare>
            </furnitype>
          </wallitemtypes>
        </furnidata>
        """;

    [Fact]
    public void ReadsBothSectionsAndTagsTheirTypes()
    {
        var items = Parse(TwoItems);

        Assert.Equal(2, items.Count);
        Assert.Equal(ItemType.Floor, items.Single(i => i.ClassId == 26).Type);
        Assert.Equal(ItemType.Wall, items.Single(i => i.ClassId == 4001).Type);
    }

    [Fact]
    public void ReadsTheFieldsThatDecideHowAnItemBehaves()
    {
        var chair = Parse(TwoItems).Single(i => i.ClassId == 26);

        Assert.Equal(26, chair.ClassId);
        Assert.Equal("chair_silo", chair.ClassName);
        Assert.Equal(1, chair.XDim);
        Assert.Equal(2, chair.YDim);
        Assert.False(chair.CanWalk);
        Assert.True(chair.CanSit);
        Assert.False(chair.CanLay);
        Assert.True(chair.CanStackOn);
        Assert.Equal(2, chair.DefaultDirection);
        Assert.Equal(75469, chair.Revision);
    }

    [Fact]
    public void ParsesHeightWithADecimalPointWhateverTheMachineLocale()
    {
        // A Danish machine parses "1.1125" as 11125 under the current culture, which would
        // put the chair a hundred tiles in the air.
        var previous = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("da-DK");
        try
        {
            Assert.Equal(1.1125, Parse(TwoItems).Single(i => i.ClassId == 26).StackHeight);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    [Fact]
    public void ReadsTextAndFlagsIncludingOnesThatAreOff()
    {
        var items = Parse(TwoItems);
        var chair = items.Single(i => i.ClassId == 26);
        var poster = items.Single(i => i.ClassId == 4001);

        Assert.Equal("Gray Dining Chair", chair.Name);
        Assert.Equal("Keep it simple.", chair.Description);
        Assert.Equal("chair", chair.CategoryName);
        Assert.Equal("area", chair.FurniLine);
        Assert.True(chair.Tradeable);
        Assert.False(chair.Recyclable);
        Assert.False(chair.Rare);
        Assert.True(poster.Rare);
        Assert.Equal(6, poster.SpecialType);
    }

    [Fact]
    public void AWallItemTakesTheGeometryDefaultsItDoesNotCarry()
    {
        var poster = Parse(TwoItems).Single(i => i.ClassId == 4001);

        Assert.Equal(1, poster.XDim);
        Assert.Equal(1, poster.YDim);
        Assert.Equal(1.0, poster.StackHeight);
    }

    [Fact]
    public void ItemsAreGroupableUntilSomethingSaysOtherwise()
    {
        // furnidata has no groupable field, so the import must not silently turn stacking off.
        Assert.All(Parse(TwoItems), item => Assert.True(item.Groupable));
    }

    [Fact]
    public void SkipsEntriesWithoutAnIdOrClassNameRatherThanThrowing()
    {
        // furnidata is a live export; one bad row should not cost the other eighteen thousand.
        var items = Parse("""
            <furnidata>
              <roomitemtypes>
                <furnitype classname="no_id"><height>1.0</height></furnitype>
                <furnitype id="notanumber" classname="bad_id"/>
                <furnitype id="7"/>
                <furnitype id="9" classname="good"/>
              </roomitemtypes>
            </furnidata>
            """);

        var only = Assert.Single(items);
        Assert.Equal("good", only.ClassName);
        Assert.Equal(9, only.ClassId);
    }

    [Fact]
    public void AMissingSectionIsNotAnError()
    {
        Assert.Empty(Parse("<furnidata><roomitemtypes/></furnidata>"));
    }
}
