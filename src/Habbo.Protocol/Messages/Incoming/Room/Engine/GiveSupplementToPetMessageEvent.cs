using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>GiveSupplementToPetMessageEvent — water a plant, or give it light</summary>
[Incoming(ClientMessages.GiveSupplementToPetMessageEvent)]
public sealed partial record GiveSupplementToPetMessageEvent(int PetId, int SupplementType);
