namespace Habbo.Silo.Abstractions.Players;

/// <summary>A player's own settings kept for them: their chat filter and how they show on Discord. Every call
/// answers the player who made it.</summary>
public interface IPlayerSettingsService
{
    Task SendFilterAsync(PlayerData player);

    Task AddFilterWordAsync(PlayerData player, string word);

    Task RemoveFilterWordAsync(PlayerData player, string word);

    Task SendDiscordPreferencesAsync(PlayerData player);

    Task SetDiscordPreferencesAsync(PlayerData player, int version, bool showHabbo, bool shareActivity, bool hideInHiddenRooms, bool allowJoining);
}
