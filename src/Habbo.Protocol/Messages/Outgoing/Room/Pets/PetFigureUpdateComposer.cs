using Habbo.Protocol.Messages.Outgoing.Inventory.Pets;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Pets;

/// <summary>PetFigureUpdateComposer — a pet's look changed</summary>
[Message(ServerMessages.PetFigureUpdateComposer)]
public sealed partial record PetFigureUpdateComposer(int RoomIndex, int PetId, PetFigureWireData Figure, bool HasSaddle, bool IsRiding);
