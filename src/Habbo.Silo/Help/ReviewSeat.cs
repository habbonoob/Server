using Habbo.Silo.Abstractions.Players;
using Orleans.Runtime;

namespace Habbo.Silo.Help;

/// <summary>A guardian's place in a review: offered, taken, voted or refused.</summary>
public sealed class ReviewSeat
{
    /// <summary>The client's statuses of a seat, which are also its result codes.</summary>
    public const int Waiting = 0;
    public const int Ok = 1;
    public const int Bad = 2;
    public const int VeryBad = 3;
    public const int Refused = 4;
    public const int Searching = 5;

    public PlayerData? Guardian { get; set; }

    /// <summary>Offered and not yet taken.</summary>
    public bool Offered { get; set; }
    public IGrainTimer? OfferTimer { get; set; }

    public int Status { get; set; } = Searching;
}
