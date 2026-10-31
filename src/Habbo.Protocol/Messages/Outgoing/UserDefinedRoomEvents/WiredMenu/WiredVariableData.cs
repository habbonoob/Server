using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu;

/// <summary>A wired variable as the client knows it (WiredVariable): its id, kind (created by a
/// user, internal, sub or smart), name, availability, what it belongs to (furni, users, the room,
/// a stack's run), what may be done with it, and, for a text connector, the texts its values stand for. From the 13 Oct
/// 2026 client on (October2026): whether it can be read only in a stack's run, and the text a value without one shows.</summary>
[WireStruct]
public sealed partial record WiredVariableData(
    string Id,
    int VariableType,
    string Name,
    int Availability,
    int Target,
    bool AlwaysAvailable,
    bool CanCreateAndDelete,
    bool HasValue,
    bool CanWriteValue,
    bool CanInterceptChanges,
    bool IsInvisible,
    [property: WireSince(ClientReleases.October2026)] bool RequiresExecutionContext,
    bool CanReadCreationTime,
    bool CanReadLastUpdateTime,
    bool HasTextConnector,
    [property: WireWhen("HasTextConnector")] IReadOnlyList<WiredTextConnectorEntryData>? TextConnector,
    [property: WireSince(ClientReleases.October2026), WireWhen("HasTextConnector")] bool? HasTextConnectorFallback,
    [property: WireSince(ClientReleases.October2026), WireWhen("HasFallbackText")] string? TextConnectorFallback)
{
    /// <summary>Whether the fallback text follows: set, inside a text connector.</summary>
    public bool HasFallbackText => HasTextConnectorFallback == true;
}

/// <summary>A value of a text-connected variable, and the text it stands for.</summary>
[WireStruct]
public sealed partial record WiredTextConnectorEntryData(int Value, string Text);

/// <summary>A furni or user holding a variable (by item id or room index), and its value.</summary>
[WireStruct]
public sealed partial record WiredVariableHolderData(int ObjectId, int Value);

/// <summary>A variable shared from another room (SharedVariable).</summary>
[WireStruct]
public sealed partial record WiredSharedVariableData(int RoomId, string RoomName, WiredVariableData Variable);

/// <summary>A placeholder shared from another room (SharedGlobalPlaceholder).</summary>
[WireStruct]
public sealed partial record WiredSharedPlaceholderData(int RoomId, string RoomName, string PlaceholderName);

/// <summary>A variable, with the hash the client caches it by.</summary>
[WireStruct]
public sealed partial record WiredVariableWithHashData(int Hash, WiredVariableData Variable);
