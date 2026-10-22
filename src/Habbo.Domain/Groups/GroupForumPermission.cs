namespace Habbo.Domain.Groups;

/// <summary>Who may do something in a group's forum, as the forum settings list it.</summary>
public enum GroupForumPermission
{
    Everyone = 0,
    Members = 1,
    Admins = 2,
    Owner = 3,
}
