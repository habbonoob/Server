using Habbo.Domain.Rooms;
using Habbo.Domain.Users;

namespace Habbo.Domain.Bots;

/// <summary>A bot a player owns: in their inventory, or standing in one of their rooms, with how it has
/// been set up to chat, walk and dance.</summary>
public sealed class Bot
{
    public const int NameMaxLength = 25;
    public const int MottoMaxLength = 127;

    public int Id { get; set; }

    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }

    public int TypeId { get; set; }
    public BotType? Type { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Motto { get; set; } = string.Empty;

    public string Figure { get; set; } = string.Empty;

    public Gender Gender { get; set; }

    /// <summary>The room it stands in; null while it is in the inventory.</summary>
    public int? RoomId { get; set; }
    public Room? Room { get; set; }

    /// <summary>Where it was put down, and which way it faces; it stands at whatever height the tile is.</summary>
    public int X { get; set; }
    public int Y { get; set; }
    public int Rotation { get; set; }

    /// <summary>What it says, one sentence each.</summary>
    public List<string> ChatLines { get; set; } = [];

    /// <summary>Says something by itself every <see cref="ChatDelaySeconds"/>.</summary>
    public bool AutoChat { get; set; }

    public int ChatDelaySeconds { get; set; }

    /// <summary>Makes up sentences out of its chat lines rather than saying them as they are.</summary>
    public bool MixSentences { get; set; }

    public bool WalksRandomly { get; set; }

    /// <summary>The dance it does; 0 when it is not dancing.</summary>
    public int DanceStyle { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
