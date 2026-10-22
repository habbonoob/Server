namespace Habbo.Silo.Games.SnowWar;

/// <summary>An offer of game tokens, each a game beyond the day's free ones: what the client calls it
/// (GET_SNOWWAR_TOKENS, GET_SNOWWAR_TOKENS2, GET_SNOWWAR_TOKENS3), how many tokens and for what.</summary>
public sealed class SnowWarTokenOffer
{
    public int Id { get; set; }
    public string LocalizationId { get; set; } = string.Empty;
    public int Tokens { get; set; }
    public int PriceInCredits { get; set; }
    public int PriceInActivityPoints { get; set; }
    public int ActivityPointType { get; set; }
}
