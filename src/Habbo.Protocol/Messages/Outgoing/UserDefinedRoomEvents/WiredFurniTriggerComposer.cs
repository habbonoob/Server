using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>2512 WiredFurniTriggerComposer — opens a trigger's dialog.</summary>
[Message(ServerMessages.WiredFurniTriggerComposer)]
public sealed partial record WiredFurniTriggerComposer(
    WiredBoxData Box,
    WiredBoxOptionsData Options,
    WiredBoxContextData Context);
