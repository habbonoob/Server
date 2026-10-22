namespace Habbo.Silo.Abstractions.Moderation;

/// <summary>The mod tool and calls for help. Each call answers the player who made it itself, and
/// tells the other moderators and whoever else a change concerns.</summary>
public interface IModerationService
{
    /// <summary>The mod tool's setup for a moderator at login (ModeratorInit); nothing for anyone else.</summary>
    Task SendInitAsync(Guid playerId);

    /// <summary>Remembers the machine a player logged in from, which a ban may then cover.</summary>
    Task RecordMachineAsync(Guid playerId, string machineId);

    /// <summary>A ban that keeps this player, or this machine, out; null when there is none.</summary>
    Task<ActiveBan?> FindBanAsync(Guid playerId, string machineId);

    Task PickIssuesAsync(Guid moderatorId, IReadOnlyList<int> issueIds, bool retryEnabled, int retryCount, string message);

    Task ReleaseIssuesAsync(Guid moderatorId, IReadOnlyList<int> issueIds);

    Task CloseIssuesAsync(Guid moderatorId, IReadOnlyList<int> issueIds, int closeReason);

    /// <summary>Closes an issue bundle, giving its reported user the default sanction for the topic.</summary>
    Task CloseWithDefaultSanctionAsync(Guid moderatorId, int issueId, IReadOnlyList<int> otherIssueIds, int topicId);

    /// <summary>What a default sanction would be for an issue's reported user, or for an account.</summary>
    Task SendSanctionPreviewAsync(Guid moderatorId, int issueId, int accountId, int topicId);

    Task SendIssueChatlogAsync(Guid moderatorId, int issueId);

    Task SendRoomChatlogAsync(Guid moderatorId, int roomId);

    Task SendUserChatlogAsync(Guid moderatorId, int accountId);

    Task SendRoomVisitsAsync(Guid moderatorId, int accountId);

    Task SendRoomInfoAsync(Guid moderatorId, int roomId);

    Task SendUserInfoAsync(Guid moderatorId, int accountId);

    /// <summary>A caution, message, mute, kick, ban or trade lock for a user.</summary>
    Task SanctionAsync(Guid moderatorId, ModerationAction action);

    /// <summary>The next step of the sanction ladder for a user.</summary>
    Task DefaultSanctionAsync(Guid moderatorId, int accountId, int topicId, string message, int? issueId);

    /// <summary>A message (or caution) to everyone in the moderator's room, and perhaps out they go.</summary>
    Task RoomActionAsync(Guid moderatorId, int actionType, string message);

    /// <summary>Takes a room in hand: locks its door, renames it, empties it.</summary>
    Task ModerateRoomAsync(Guid moderatorId, int roomId, bool lockDoor, bool changeTitle, bool kickUsers);

    Task SavePreferencesAsync(Guid moderatorId, int x, int y, int width, int height);

    /// <summary>A call for help.</summary>
    Task ReportAsync(Guid reporterId, CallForHelpReport report);

    /// <summary>Files a report for the moderators on the reporter's behalf, as a system does when it hands
    /// on what it could not settle itself: no checks of the reporter's, and nothing said to them.</summary>
    Task ForwardAsync(Guid reporterId, CallForHelpReport report);

    Task SendPendingCallsAsync(Guid playerId);

    Task DeletePendingCallsAsync(Guid playerId);

    Task SendMyReportStatusAsync(Guid playerId);

    Task SendMySanctionStatusAsync(Guid playerId);

    Task AppealAsync(Guid playerId, int issueId);
}
