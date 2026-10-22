using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Vault;

/// <summary>IncomeRewardData — earnings waiting in one category: credits, duckets, or a product</summary>
[WireStruct]
public sealed partial record IncomeRewardData(byte RewardCategory, byte RewardType, int Amount, string ProductCode)
{
    /// <summary>The earnings window's categories ("earnings.*.label").</summary>
    public const byte DailyGift = 1;
    public const byte Achievements = 2;
    public const byte Marketplace = 3;
    public const byte HabboClub = 4;
    public const byte LevelProgression = 5;
    public const byte BonusBag = 7;
    public const byte Donations = 8;
    public const byte SurpriseBoxes = 9;
    public const byte Snowstorm = 10;
    public const byte Games = 11;
    public const byte WiredChests = 12;
    public const byte Agencies = 13;

    /// <summary>Claiming every category at once, as the window's "claim all" sends it.</summary>
    public const byte AllCategories = 255;

    /// <summary>What the earnings are, as the window adds them up.</summary>
    public const byte Duckets = 0;
    public const byte Credits = 1;
}
