using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Clothing;

/// <summary>272 FigureSetIdAddedComposer.</summary>
[Message(ServerMessages.FigureSetIdAddedComposer)]
public sealed partial record FigureSetIdAddedComposer(int SetId);
