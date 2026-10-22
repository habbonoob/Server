using Habbo.Domain.Users;

namespace Habbo.Domain.Bots;

/// <summary>A kind of bot the shop sells: what it is called and wears when it arrives, and the skills
/// its menu offers (the client's bot skill ids).</summary>
public sealed class BotType
{
    public const int CodeMaxLength = 64;

    public int Id { get; set; }

    /// <summary>The seed's name for the kind, such as "bartender".</summary>
    public string Code { get; set; } = string.Empty;

    public string DefaultName { get; set; } = string.Empty;

    public string DefaultMotto { get; set; } = string.Empty;

    public Gender Gender { get; set; }

    public List<int> Skills { get; set; } = [];

    /// <summary>For a bot that serves: the hand items it serves, each asked for by its name.</summary>
    public List<int> Beverages { get; set; } = [];

    /// <summary>A bot that keeps a log of who visited and tells the room owner.</summary>
    public bool LogsVisitors { get; set; }
}
