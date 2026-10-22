namespace Habbo.Domain.Furniture;

/// <summary>The client's furni categories (its FurniCategory enum), which every inventory, trade and chest entry
/// carries. The client branches on them: a wallpaper, floor or landscape is applied to the room rather than placed,
/// a post-it is stacked by count, a poster is drawn from its number, a pet item is offered for use on a pet, a chest
/// shows its colour. Habbo sends <see cref="Default"/> for everything else.</summary>
public static class FurniCategory
{
    public const int Default = 1;
    public const int WallPaper = 2;
    public const int Floor = 3;
    public const int Landscape = 4;
    public const int PostIt = 5;
    public const int Poster = 6;
    public const int SoundSet = 7;
    public const int TraxSong = 8;
    public const int Present = 9;
    public const int EcotronBox = 10;
    public const int Trophy = 11;
    public const int CreditFurni = 12;
    public const int PetShampoo = 13;
    public const int PetCustomPart = 14;
    public const int PetCustomPartShampoo = 15;
    public const int PetSaddle = 16;
    public const int GuildFurni = 17;
    public const int MonsterPlantSeed = 19;
    public const int MonsterPlantRevival = 20;
    public const int MonsterPlantRebreed = 21;
    public const int MonsterPlantFertilize = 22;
    public const int FigurePurchasableSet = 23;
    public const int FurniChest = 24;
    public const int CoinsChest = 25;

    /// <summary>The category of an item of this kind: by its behaviour where one says it, otherwise by its class name,
    /// which is all that tells a horse's dye from its saddle or a monster plant's revival potion from its compost.</summary>
    public static int Of(ItemDefinition definition)
    {
        var name = BaseName(definition.ClassName);
        return definition.Logic.ToLowerInvariant() switch
        {
            "furniture_stickie" => PostIt,
            "furniture_song_disk" => TraxSong,
            "furniture_present" => Present,
            "furniture_ecotron_box" => EcotronBox,
            "furniture_trophy" => Trophy,
            "furniture_credit" or "furniture_nft_credit" => CreditFurni,
            "furniture_guild_customized" => GuildFurni,
            "furniture_monsterplant_seed" => MonsterPlantSeed,
            "furniture_purchasable_clothing" => FigurePurchasableSet,
            "furniture_furnichest" => FurniChest,
            "furniture_coinschest" => CoinsChest,
            _ => name switch
            {
                "wallpaper" => WallPaper,
                "floor" => Floor,
                "landscape" => Landscape,
                "poster" => Poster,
                "song_disk" => TraxSong,
                _ when name.StartsWith("post_it", StringComparison.Ordinal) || name.StartsWith("post.it", StringComparison.Ordinal) => PostIt,
                _ when name.StartsWith("sound_set_", StringComparison.Ordinal) => SoundSet,
                _ when name.StartsWith("horse_dye", StringComparison.Ordinal) => PetShampoo,
                _ when name.StartsWith("horse_hairstyle", StringComparison.Ordinal) => PetCustomPart,
                _ when name.StartsWith("horse_hairdye", StringComparison.Ordinal) => PetCustomPartShampoo,
                _ when name.StartsWith("horse_saddle", StringComparison.Ordinal) => PetSaddle,
                _ when name.StartsWith("mnstr_revival", StringComparison.Ordinal) => MonsterPlantRevival,
                _ when name.StartsWith("mnstr_rebreed", StringComparison.Ordinal) => MonsterPlantRebreed,
                _ when name.StartsWith("mnstr_fert", StringComparison.Ordinal) => MonsterPlantFertilize,
                _ => Default,
            },
        };
    }

    /// <summary>"rare_dragonlamp*4" is a colour of "rare_dragonlamp".</summary>
    private static string BaseName(string className)
    {
        var colour = className.IndexOf('*');
        return colour < 0 ? className : className[..colour];
    }
}
