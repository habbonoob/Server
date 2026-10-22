using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.GroupForums;

/// <summary>ForumDataMessageComposer</summary>
[Message(ServerMessages.ForumDataMessageComposer)]
public sealed partial record ForumDataMessageComposer(ForumData Forum, int ReadPermissions, int PostMessagePermissions, int PostThreadPermissions, int ModeratePermissions, string ReadPermissionError, string PostMessagePermissionError, string PostThreadPermissionError, string ModeratePermissionError, string ReportPermissionError, bool CanChangeSettings, bool IsStaff);
