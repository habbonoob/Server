namespace Habbo.Silo.Abstractions.Players;

/// <summary>Lists users by class for staff and ambassadors (the client's :uc and :anew).</summary>
public interface IUserClassificationService
{
    /// <summary>The users of a class in the room.</summary>
    Task SendRoomAsync(PlayerData player, IReadOnlyList<Guid> roomUsers, string classType);

    /// <summary>The users of a class online anywhere in the hotel.</summary>
    Task SendHotelAsync(PlayerData player, string classType);
}
