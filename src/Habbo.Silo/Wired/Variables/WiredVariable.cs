using System.Globalization;
using System.Text;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Variables;

/// <summary>What a variable belongs to (the client's VariableTarget and the variable boxes'
/// merged sources): furni, users, the room, or one run of a stack.</summary>
public static class WiredVariableTargets
{
    public const int Furni = 0;
    public const int User = 1;
    public const int Global = -10;
    public const int Context = -20;
}

/// <summary>How long a variable's values last (wiredfurni.params.variables.availability.N).</summary>
public static class WiredVariableAvailability
{
    public const int WhileUserInRoom = 0;
    public const int WhileRoomActive = 1;
    public const int Permanent = 10;
    public const int PermanentShared = 11;
    public const int Reference = 20;

    public static bool IsPermanent(int availability) => availability is Permanent or PermanentShared;
}

/// <summary>Where a variable comes from (wiredfurni.params.variables.idtype.N).</summary>
public static class WiredVariableKinds
{
    public const int CreatedByUser = 0;
    public const int Internal = 1;
    public const int SubVariable = 2;
    public const int Smart = 3;
}

/// <summary>A wired variable: what the client is told of it, and how it may be used.</summary>
public sealed record WiredVariable(
    string Id,
    int Kind,
    string Name,
    int Availability,
    int Target,
    bool AlwaysAvailable,
    bool CanCreateAndDelete,
    bool HasValue,
    bool CanWriteValue,
    bool CanInterceptChanges,
    bool IsInvisible,
    bool CanReadCreationTime,
    bool CanReadLastUpdateTime)
{
    /// <summary>For a variable another room shares, and a reference to one: the room its values are
    /// kept in; and, for a reference, the id they are kept under there.</summary>
    public int? SourceRoomId { get; init; }
    public string? SourceId { get; init; }

    /// <summary>For a sub-variable: the variable it is part of, and how its value follows from that
    /// variable's value and times.</summary>
    public string? SubOf { get; init; }
    public Func<int?, WiredVariableValue?, int?>? Compute { get; init; }

    /// <summary>The variable an echo reads and writes through; null for any other.</summary>
    public string? EchoOf { get; init; }

    /// <summary>The texts a text connector gives its values; none for most variables.</summary>
    public IReadOnlyDictionary<int, string> TextConnector { get; init; } = new Dictionary<int, string>();

    public WiredVariableData ToWire() => new(
        Id, Kind, Name, Availability, Target, AlwaysAvailable, CanCreateAndDelete, HasValue, CanWriteValue,
        CanInterceptChanges, IsInvisible, CanReadCreationTime, CanReadLastUpdateTime,
        TextConnector.Count > 0,
        TextConnector.Count > 0 ? TextConnector.Select(t => new WiredTextConnectorEntryData(t.Key, t.Value)).ToList() : null);

    /// <summary>A hash of everything the client is told, which it caches the variable by: stable
    /// across restarts, so a client's cache stays good while nothing changed.</summary>
    public int Hash
    {
        get
        {
            var text = new StringBuilder()
                .Append(Id).Append('|').Append(Kind).Append('|').Append(Name).Append('|').Append(Availability).Append('|')
                .Append(Target).Append('|').Append(AlwaysAvailable).Append(CanCreateAndDelete).Append(HasValue)
                .Append(CanWriteValue).Append(CanInterceptChanges).Append(IsInvisible).Append(CanReadCreationTime)
                .Append(CanReadLastUpdateTime);
            foreach (var (value, name) in TextConnector.OrderBy(t => t.Key))
                text.Append('|').Append(value.ToString(CultureInfo.InvariantCulture)).Append('=').Append(name);
            return StableHash(text.ToString());
        }
    }

    /// <summary>FNV-1a, which unlike string.GetHashCode is the same in every process.</summary>
    public static int StableHash(string text)
    {
        unchecked
        {
            var hash = (int)2166136261;
            foreach (var c in text)
                hash = (hash ^ c) * 16777619;
            return hash;
        }
    }
}

/// <summary>Who holds a variable: a furni, a user, the room, or a stack's run (the last two need no holder).</summary>
public readonly record struct WiredHolder(int Target, RoomItemStore.PlacedItem? Item = null, RoomEntityStore.RoomEntity? User = null)
{
    public static WiredHolder Of(RoomItemStore.PlacedItem item) => new(WiredVariableTargets.Furni, Item: item);
    public static WiredHolder Of(RoomEntityStore.RoomEntity user) => new(WiredVariableTargets.User, User: user);
    public static readonly WiredHolder Room = new(WiredVariableTargets.Global);
    public static readonly WiredHolder Context = new(WiredVariableTargets.Context);
}

/// <summary>A held variable's value, and when it was given and last changed.</summary>
public sealed class WiredVariableValue(int value, DateTimeOffset created)
{
    public int Value { get; set; } = value;
    public DateTimeOffset Created { get; set; } = created;
    public DateTimeOffset Updated { get; set; } = created;
}

/// <summary>What happened to a held variable, for the variable-changed trigger.</summary>
public enum WiredVariableChange
{
    Created,
    Changed,
    Deleted,
}
