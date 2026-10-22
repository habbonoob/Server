namespace Habbo.Domain.Chat;

/// <summary>A chat bubble, as the client's chat style library describes it. The server needs the
/// same rules the client's selector applies, so it can refuse a bubble the player could not have
/// picked.</summary>
public sealed class ChatStyle
{
    /// <summary>Ids from here up to <see cref="LastNftStyle"/> are NFT bubbles, owned through collectibles.</summary>
    public const int FirstNftStyle = 1000;

    public const int LastNftStyle = 9999;

    /// <summary>Ids from here up are bought.</summary>
    public const int FirstPurchasableStyle = 10000;

    public int Id { get; set; }

    public string AssetId { get; set; } = string.Empty;

    /// <summary>Used by the hotel itself (bots, notifications); never a player's choice.</summary>
    public bool SystemStyle { get; set; }

    public bool Purchasable { get; set; }

    public bool HcOnly { get; set; }

    /// <summary>Staff may use it whatever else it requires.</summary>
    public bool StaffOverrideable { get; set; }

    public bool AmbassadorOnly { get; set; }

    public bool IsNft => Id is >= FirstNftStyle and <= LastNftStyle;
}
