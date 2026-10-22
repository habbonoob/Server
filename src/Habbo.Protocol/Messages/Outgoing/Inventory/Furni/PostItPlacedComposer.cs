using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Furni;

/// <summary>471 PostItPlacedComposer — a note went up from a pad; the pad has this many left.</summary>
[Message(ServerMessages.PostItPlacedComposer)]
public sealed partial record PostItPlacedComposer(int PadId, int ItemsLeft);
