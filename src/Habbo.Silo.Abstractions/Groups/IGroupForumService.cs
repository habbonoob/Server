namespace Habbo.Silo.Abstractions.Groups;

/// <summary>Group forums. Every call answers the player who made it itself.</summary>
public interface IGroupForumService
{
    Task SendForumsListAsync(Guid playerId, int listCode, int startIndex, int amount);

    Task SendForumAsync(Guid playerId, int groupId);

    /// <summary>The context menu of a group's furniture.</summary>
    Task SendFurniContextMenuInfoAsync(Guid playerId, int itemId, int groupId);

    Task UpdateSettingsAsync(Guid playerId, int groupId, int read, int postMessage, int postThread, int moderate);

    Task SendThreadsAsync(Guid playerId, int groupId, int startIndex, int amount);

    Task SendThreadAsync(Guid playerId, int groupId, int threadId);

    Task SendMessagesAsync(Guid playerId, int groupId, int threadId, int startIndex, int amount);

    /// <summary>Starts a thread (thread 0) or replies in one.</summary>
    Task PostAsync(Guid playerId, int groupId, int threadId, string subject, string message);

    Task UpdateThreadAsync(Guid playerId, int groupId, int threadId, bool sticky, bool locked);

    Task ModerateThreadAsync(Guid playerId, int groupId, int threadId, int state);

    Task ModerateMessageAsync(Guid playerId, int groupId, int threadId, int messageId, int state);

    Task MarkReadAsync(Guid playerId, IReadOnlyList<ForumReadMark> marks);

    Task SendUnreadCountAsync(Guid playerId);
}
