using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>164 WiredFurniSelectorComposer — opens a selector's dialog.</summary>
[Message(ServerMessages.WiredFurniSelectorComposer)]
public sealed partial record WiredFurniSelectorComposer(
    WiredBoxData Box,
    bool IsFilter,
    bool IsInvert,
    WiredBoxOptionsData Options,
    WiredBoxContextData Context);
