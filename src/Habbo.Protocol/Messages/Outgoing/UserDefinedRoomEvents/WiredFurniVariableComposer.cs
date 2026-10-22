using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>88 WiredFurniVariableComposer — opens a variable box's dialog.</summary>
[Message(ServerMessages.WiredFurniVariableComposer)]
public sealed partial record WiredFurniVariableComposer(
    WiredBoxData Box,
    WiredBoxOptionsData Options,
    WiredBoxContextData Context);
