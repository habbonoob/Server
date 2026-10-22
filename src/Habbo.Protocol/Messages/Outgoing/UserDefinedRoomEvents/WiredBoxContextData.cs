using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>How every wired dialog ends: its context entries, then the int parameters a new
/// box starts with, which the dialog resets to.</summary>
[WireStruct]
public sealed partial record WiredBoxContextData(
    IReadOnlyList<WiredContextEntryData> Entries,
    IReadOnlyList<int> DefaultIntParams);
