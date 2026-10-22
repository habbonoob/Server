using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Navigator;

/// <summary>The hotel's polls. Every call answers the player who made it.</summary>
public interface IPollService
{
    /// <summary>Offers the player the poll the room runs, if they have not dealt with it yet.</summary>
    Task OfferAsync(PlayerData player, int roomId);

    Task StartAsync(PlayerData player, int pollId);

    Task RejectAsync(PlayerData player, int pollId);

    Task AnswerAsync(PlayerData player, int pollId, int questionId, IReadOnlyList<string> answers);
}
