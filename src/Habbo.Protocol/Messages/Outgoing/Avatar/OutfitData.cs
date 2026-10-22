using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Avatar;

[WireStruct]
public sealed partial record OutfitData(int SlotId, string Figure, string Gender);
