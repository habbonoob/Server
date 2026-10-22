using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Recycler;

/// <summary>RecyclerPrizeLevelData — a prize level and its odds: one in the denominator</summary>
[WireStruct]
public sealed partial record RecyclerPrizeLevelData(int PrizeLevelId, int ProbabilityDenominator, IReadOnlyList<RecyclerPrizeData> Prizes);
