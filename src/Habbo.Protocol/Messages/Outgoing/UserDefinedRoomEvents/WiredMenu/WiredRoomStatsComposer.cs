using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu;

/// <summary>3152 WiredRoomStatsComposer — the monitor's statistics (WiredRoomStatsData).</summary>
[Message(ServerMessages.WiredRoomStatsComposer)]
public sealed partial record WiredRoomStatsComposer(
    double ExecutionCost,
    double ExecutionCostCap,
    bool IsHeavy,
    int FloorItemCount,
    int FloorItemCap,
    int WallItemCount,
    int WallItemCap,
    int PermanentFurniVariables,
    int MaxPermanentFurniVariables,
    int PermanentUserVariables,
    int MaxPermanentUserVariables,
    int PermanentGlobalVariables,
    int MaxPermanentGlobalVariables);
