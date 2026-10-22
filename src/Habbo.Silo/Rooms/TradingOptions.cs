namespace Habbo.Silo.Rooms;

/// <summary>Trading in the hotel (config section "Trading").</summary>
public sealed class TradingOptions
{
    /// <summary>Whether players may trade at all.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>The most items one side may offer in a trade.</summary>
    public int ItemLimit { get; set; } = 100;

    /// <summary>Whether giving in a trade needs an activated email address. Without one a player still
    /// receives, but offers nothing, and both are told.</summary>
    public bool RequireVerifiedEmail { get; set; }
}
