using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;

/// <summary>GameLevelData — the arena's floor and furni</summary>
[WireStruct]
public sealed partial record GameLevelData(int Width, int Height, string HeightMap, IReadOnlyList<FuseObjectData> FuseObjects);
