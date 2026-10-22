namespace Habbo.Silo.Abstractions.Friends;

/// <summary>The console: messages between friends and in group chats, kept for whoever was away
/// and for scrolling back, and what the friend list does besides listing friends — following them,
/// inviting them, finding people. Every call answers the player who made it itself.</summary>
public interface IMessengerService
{
    /// <summary>A console message to a friend, or to a group chat (a negative id).</summary>
    Task SendMessageAsync(Guid playerId, int chatId, string text, int confirmationId);

    /// <summary>The messages that arrived while the player was away.</summary>
    Task DeliverPendingAsync(Guid playerId);

    /// <summary>Older messages of a conversation, before the one the client names (none: the newest).</summary>
    Task SendHistoryAsync(Guid playerId, int chatId, string beforeMessageId);

    /// <summary>Goes to the room a friend is in.</summary>
    Task FollowAsync(Guid playerId, int accountId);

    /// <summary>Goes to the room a user is in, by name.</summary>
    Task VisitAsync(Guid playerId, string userName);

    /// <summary>Goes somewhere busy, to meet people.</summary>
    Task FindNewFriendsAsync(Guid playerId);

    /// <summary>Users whose names start with the query: the player's friends, and the rest.</summary>
    Task SearchAsync(Guid playerId, string query);

    /// <summary>Invites friends to the room the player is in.</summary>
    Task InviteAsync(Guid playerId, IReadOnlyList<int> accountIds, string text);

    /// <summary>Marks a friend (heart, smile, bobba or nothing).</summary>
    Task SetRelationshipAsync(Guid playerId, int accountId, int status);

    /// <summary>The whole friend list again, as an update.</summary>
    Task RefreshFriendListAsync(Guid playerId);
}
