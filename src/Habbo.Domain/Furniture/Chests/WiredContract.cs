namespace Habbo.Domain.Furniture.Chests;

/// <summary>What a contract furni says a wired transaction takes and gives: a payment what the user pays (any one of
/// its rules), a trade that and what they get for it, a reward only what they get.</summary>
public sealed class WiredContract
{
    /// <summary>The kinds of contract, as the client numbers them.</summary>
    public const short Payment = 0;
    public const short Trade = 1;
    public const short Reward = 2;

    /// <summary>How a payment is made: anything counts as a donation, or one of the rules must be met.</summary>
    public const short DonationMode = 0;
    public const short SpecificMode = 1;

    /// <summary>What the contract dialog allows.</summary>
    public const int MaxRules = 3;
    public const int MaxNodesPerRule = 5;
    public const int MaxCredits = 100000;
    public const int MaxFurni = 500;
    public const int ReceiveTextMaxLength = 60;
    public const int RewardTextMaxLength = 200;

    /// <summary>The payment layouts the dialog offers.</summary>
    public static IReadOnlyList<string> LayoutTypes { get; } = ["generic", "games"];

    /// <summary>The earnings categories a reward's credits may land in.</summary>
    public static IReadOnlyList<short> RewardCategories { get; } = [11, 13];

    /// <summary>The contract furni it belongs to.</summary>
    public int ItemId { get; set; }

    public short Type { get; set; }

    public List<ContractRule>? YouGive { get; set; }

    public ContractRule? YouGet { get; set; }

    public short PaymentMode { get; set; }

    public string ReceiveText { get; set; } = string.Empty;

    public string LayoutType { get; set; } = string.Empty;

    public short RewardCategory { get; set; }

    public bool ShowDialog { get; set; }

    public string RewardText { get; set; } = string.Empty;

    /// <summary>A contract nobody has written yet: nothing to give or get, in the shape its kind has.</summary>
    public static WiredContract Blank(int itemId, short type) => new()
    {
        ItemId = itemId,
        Type = type,
        YouGive = type is Payment or Trade ? [] : null,
        YouGet = type is Trade or Reward ? new ContractRule() : null,
        LayoutType = LayoutTypes[0],
        RewardCategory = RewardCategories[0],
    };
}
