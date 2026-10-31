using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Quests;

/// <summary>Seasonal quests: time-limited campaigns, each a chain of quests, and the advent calendar the client draws
/// from those its prefix starts, a day each. A player takes on one quest at a time; what they then do counts towards
/// it, and finishing it pays its reward. Every call answers the player who made it.</summary>
public interface ISeasonalQuestService
{
    /// <summary>The running campaigns, each with the quest the player is on in it (SeasonalQuestsMessage).</summary>
    Task SendSeasonalQuestsAsync(PlayerData player);

    /// <summary>Takes on the quest, when it is the one the player is on in a running campaign; any other they had taken on
    /// is put down.</summary>
    Task AcceptAsync(PlayerData player, int questId);

    /// <summary>Puts down the quest taken on: that one, or (null) whichever it is.</summary>
    Task RejectAsync(PlayerData player, int? questId);

    /// <summary>Counts something the player did towards the seasonal quest they are on, when it counts that: a quest type
    /// (WEAR_BADGE, WAVE, ...) and, for one narrowed to it, what it was done with (the badge code).</summary>
    Task ProgressAsync(Guid playerId, string questType, string parameter, int amount);
}
