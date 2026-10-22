using Habbo.Domain.Furniture;

namespace Habbo.Silo.Tests;

/// <summary>The category the client branches on, from what an item is.</summary>
public class FurniCategoryTests
{
    private static int Of(string className, string logic = ItemDefinition.BasicLogic) =>
        FurniCategory.Of(new ItemDefinition { ClassName = className, Logic = logic });

    [Fact]
    public void PlainFurniIsTheDefault() => Assert.Equal(FurniCategory.Default, Of("plant_pineapple"));

    [Fact]
    public void AColourIsCategorisedAsItsItem() => Assert.Equal(FurniCategory.Default, Of("rare_dragonlamp*4"));

    [Fact]
    public void RoomDecorationIsAppliedNotPlaced()
    {
        Assert.Equal(FurniCategory.WallPaper, Of("wallpaper"));
        Assert.Equal(FurniCategory.Floor, Of("floor"));
        Assert.Equal(FurniCategory.Landscape, Of("landscape"));
    }

    [Fact]
    public void ABehaviourSaysTheCategory()
    {
        Assert.Equal(FurniCategory.Trophy, Of("trophy_gold", "furniture_trophy"));
        Assert.Equal(FurniCategory.CreditFurni, Of("CF_10_coin_gold", "furniture_credit"));
        Assert.Equal(FurniCategory.PostIt, Of("post_it", "furniture_stickie"));
        Assert.Equal(FurniCategory.CoinsChest, Of("chest_coins", "furniture_coinschest"));
    }

    [Fact]
    public void AHorsesProductsAreToldApartByName()
    {
        Assert.Equal(FurniCategory.PetShampoo, Of("horse_dye_3", "furniture_pet_customization"));
        Assert.Equal(FurniCategory.PetCustomPart, Of("horse_hairstyle_2", "furniture_pet_customization"));
        Assert.Equal(FurniCategory.PetCustomPartShampoo, Of("horse_hairdye_7", "furniture_pet_customization"));
        Assert.Equal(FurniCategory.PetSaddle, Of("horse_saddle1", "furniture_pet_customization"));
    }
}
