namespace Habbo.Domain.Catalog;

/// <summary>What a catalog product hands over.</summary>
public enum CatalogProductType
{
    /// <summary>An item of furniture: floor or wall, as its definition says.</summary>
    Furniture = 0,

    /// <summary>A badge, whose code is the product's extra parameter.</summary>
    Badge = 1,

    /// <summary>An avatar effect, by its id, for the product's duration.</summary>
    Effect = 2,

    /// <summary>A chat bubble style, by its id.</summary>
    ChatStyle = 3,

    /// <summary>Habbo Club membership: the level in the class id, the months in the amount, and any
    /// extra days in the duration.</summary>
    Club = 4,

    /// <summary>A bot, whose kind is the class id and whose look is the extra parameter.</summary>
    Bot = 5,

    /// <summary>A pet, whose kind is the class id; its name, palette and colour come with the purchase.</summary>
    Pet = 6,
}
