namespace Habbo.Domain.Groups;

/// <summary>Who may join a group, as the client's settings name it (its guild type).</summary>
public enum GroupState
{
    /// <summary>Anyone may join.</summary>
    Open = 0,

    /// <summary>Anyone may ask; an admin decides.</summary>
    Exclusive = 1,

    /// <summary>Nobody may join.</summary>
    Private = 2,
}
