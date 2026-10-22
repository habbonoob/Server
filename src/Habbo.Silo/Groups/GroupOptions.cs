using Habbo.Domain.Users;

namespace Habbo.Silo.Groups;

/// <summary>The hotel's rules for groups (config section "Groups").</summary>
public sealed class GroupOptions
{
    /// <summary>What buying a group costs, in credits.</summary>
    public int CreationCost { get; set; } = 10;

    /// <summary>The membership needed to buy a group (the client asks for VIP, which is the club).</summary>
    public ClubLevel CreationClubLevel { get; set; } = ClubLevel.Club;

    public int NameMinLength { get; set; } = 3;

    /// <summary>How many members a group can hold.</summary>
    public int MemberLimit { get; set; } = 50000;

    /// <summary>How many groups a player may be in, without and with the club.</summary>
    public int MembershipLimit { get; set; } = 50;

    public int ClubMembershipLimit { get; set; } = 400;

    /// <summary>Members per page of the member list.</summary>
    public int MembersPageSize { get; set; } = 14;

    /// <summary>The badge a new group's editor starts from, base first. The editor offers no way to take a
    /// base off again, so starting with one is what keeps every badge bought with a base.</summary>
    public List<DefaultBadgeLayer> DefaultBadge { get; set; } = [];
}
