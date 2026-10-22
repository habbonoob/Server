using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Navigator;

/// <summary>Room competitions: entering one's room, voting for others', and finding the rooms entered. Every call answers
/// the player who made it.</summary>
public interface IRoomCompetitionService
{
    /// <summary>On going into a room: to its owner, how entering it stands; to others, whether they may vote for it.</summary>
    Task SendRoomInfoAsync(PlayerData player, int roomId);

    Task SubmitAsync(PlayerData player, int roomId, string goalCode, int step);

    Task VoteAsync(PlayerData player, int roomId, string goalCode);

    Task SendIsPartOfAsync(PlayerData player, string goalCode);

    Task ForwardToEntryAsync(PlayerData player, string goalCode, int roomId);

    Task ForwardToRandomEntryAsync(PlayerData player, string goalCode);

    /// <summary>To the player's newest room that could be entered in the competition on, or an alert when they have none.</summary>
    Task ForwardToSubmittableRoomAsync(PlayerData player);

    /// <summary>The seconds until a time of the hotel's, written "yyyy-MM-dd HH:mm".</summary>
    Task SendSecondsUntilAsync(PlayerData player, string time);
}
