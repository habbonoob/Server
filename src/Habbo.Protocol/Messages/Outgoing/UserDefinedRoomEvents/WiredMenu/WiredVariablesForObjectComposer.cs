using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu;

/// <summary>2311 WiredVariablesForObjectComposer — what a furni, user or the room holds
/// (WiredObjectInspectionData): its kind, which one, its variables, and for a furni, the wired
/// boxes that pick it.</summary>
[Message(ServerMessages.WiredVariablesForObjectComposer)]
public sealed partial record WiredVariablesForObjectComposer(
    int Target,
    [property: WireWhen("IsFurni")] int? ObjectId,
    [property: WireWhen("IsUser")] int? UserIndex,
    IReadOnlyList<WiredVariableValueData> Values,
    [property: WireWhen("IsFurni")] IReadOnlyList<int>? ConfiguredInWireds)
{
    public const int FurniTarget = 0;
    public const int UserTarget = 1;

    public bool IsFurni => Target == FurniTarget;
    public bool IsUser => Target == UserTarget;
}
