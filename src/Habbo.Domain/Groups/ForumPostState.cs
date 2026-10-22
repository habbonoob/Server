namespace Habbo.Domain.Groups;

/// <summary>Whether a thread or message shows, as the client's ThreadStateEnum numbers it.</summary>
public enum ForumPostState : byte
{
    Default = 0,
    RestoredByAdmin = 1,
    HiddenByAdmin = 10,
    PermanentlyHiddenByModerator = 20,
}
