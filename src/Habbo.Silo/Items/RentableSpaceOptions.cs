namespace Habbo.Silo.Items;

/// <summary>Rentable spaces (config section "RentableSpaces").</summary>
public sealed class RentableSpaceOptions
{
    public bool Enabled { get; set; }

    /// <summary>What renting a space costs, and for how long it is rented.</summary>
    public int PriceInCredits { get; set; }
    public int RentSeconds { get; set; }

    /// <summary>Whether only club members may rent, and whether a player may rent one space at a time.</summary>
    public bool ClubRequired { get; set; }
    public bool OneSpacePerPlayer { get; set; }

    /// <summary>What a space's custom parameters say when it rents out the whole room rather than its own tiles.</summary>
    public string WholeRoomParameter { get; set; } = string.Empty;
}
