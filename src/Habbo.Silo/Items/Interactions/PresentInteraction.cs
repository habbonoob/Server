namespace Habbo.Silo.Items.Interactions;

/// <summary>A wrapped present: its data is the card (message, sender, what it holds) as a map, and its
/// extra the box and ribbon. Opening it hands over what furni_gifts says it holds.</summary>
[FurniInteraction("furniture_present")]
public sealed class PresentInteraction : FurniInteraction
{
    // the card's keys, as the client reads them
    public const string MessageKey = "MESSAGE";
    public const string ProductCodeKey = "PRODUCT_CODE";
    public const string ExtraParamKey = "EXTRA_PARAM";
    public const string PurchaserNameKey = "PURCHASER_NAME";
    public const string PurchaserFigureKey = "PURCHASER_FIGURE";

    /// <summary>The extra packs the box and the ribbon as box * 1000 + ribbon.</summary>
    public const int BoxFactor = 1000;
}
