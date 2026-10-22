using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Badges;

[WireStruct]
public sealed partial record BadgeLevelLimitData(int Level, int Limit);
