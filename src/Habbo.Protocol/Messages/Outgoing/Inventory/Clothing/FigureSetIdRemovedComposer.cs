using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Clothing;

/// <summary>1760 FigureSetIdRemovedComposer.</summary>
[Message(ServerMessages.FigureSetIdRemovedComposer)]
public sealed partial record FigureSetIdRemovedComposer(int SetId);
