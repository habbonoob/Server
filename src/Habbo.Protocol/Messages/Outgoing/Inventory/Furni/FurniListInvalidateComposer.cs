using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Furni;

/// <summary>FurniListInvalidateComposer — the inventory changed: the client asks for it again</summary>
[Message(ServerMessages.FurniListInvalidateComposer)]
public sealed partial record FurniListInvalidateComposer;
