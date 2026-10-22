using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>493 WiredFurniActionComposer — opens an effect's dialog. The delay is in pulses of
/// half a second.</summary>
[Message(ServerMessages.WiredFurniActionComposer)]
public sealed partial record WiredFurniActionComposer(
    WiredBoxData Box,
    int DelayInPulses,
    WiredBoxOptionsData Options,
    WiredBoxContextData Context);
