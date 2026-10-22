using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Clothing;

/// <summary>3142 FigureSetIdsComposer — the clothing sets the player unlocked, and the furni they came from.</summary>
[Message(ServerMessages.FigureSetIdsComposer)]
public sealed partial record FigureSetIdsComposer(IReadOnlyList<int> FigureSetIds, IReadOnlyList<string> BoundFurnitureNames);
