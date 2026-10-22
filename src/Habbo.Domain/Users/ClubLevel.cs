namespace Habbo.Domain.Users;

/// <summary>A player's Habbo Club standing, as the client's HabboClubLevelEnum names it.
/// The client's own HasClub and HasVip both answer "level is at least Club", so VIP buys
/// nothing there; it is kept apart because the catalog still prices offers by it.</summary>
public enum ClubLevel
{
    None = 0,
    Club = 1,
    Vip = 2,
}
