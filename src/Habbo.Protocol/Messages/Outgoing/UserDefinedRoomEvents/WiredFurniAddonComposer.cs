using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>1942 WiredFurniAddonComposer — opens an add-on's dialog.</summary>
[Message(ServerMessages.WiredFurniAddonComposer)]
public sealed partial record WiredFurniAddonComposer(
    WiredBoxData Box,
    WiredBoxOptionsData Options,
    WiredBoxContextData Context);
