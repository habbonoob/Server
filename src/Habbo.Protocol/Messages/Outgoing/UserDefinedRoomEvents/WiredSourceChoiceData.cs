using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>The sources one target slot of a wired dialog may be set to: for a furni slot, say,
/// the picked furni or those a selector chose.</summary>
[WireStruct]
public sealed partial record WiredSourceChoiceData(IReadOnlyList<int> Sources);
