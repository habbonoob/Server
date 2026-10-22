using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>1688 WiredFurniConditionComposer — opens a condition's dialog. The quantifier is whether
/// all or any of the condition's targets must pass; its type says which of those the dialog offers.</summary>
[Message(ServerMessages.WiredFurniConditionComposer)]
public sealed partial record WiredFurniConditionComposer(
    WiredBoxData Box,
    int Quantifier,
    WiredBoxOptionsData Options,
    byte QuantifierType,
    bool IsInvert,
    WiredBoxContextData Context);
