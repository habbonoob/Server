using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Furniture;

/// <summary>PresentOpenedMessageComposer — what a present held, and where it went</summary>
[Message(ServerMessages.PresentOpenedMessageComposer)]
public sealed partial record PresentOpenedMessageComposer(string ItemType, int ClassId, string ProductCode, int PlacedItemId, string PlacedItemType, bool PlacedInRoom, string PetFigure);
