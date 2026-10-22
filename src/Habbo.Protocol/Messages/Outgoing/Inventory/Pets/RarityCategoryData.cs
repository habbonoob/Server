using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Pets;

/// <summary>RarityCategoryData — how likely a category of breeds is, and which breeds it holds</summary>
[WireStruct]
public sealed partial record RarityCategoryData(int Chance, IReadOnlyList<int> Breeds);
